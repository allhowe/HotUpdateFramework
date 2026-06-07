# HybridCLR + YooAsset + CDN 热更新框架

这个项目在 `Assets/HotUpdateFramework` 下提供了一套轻量热更新框架，用来串联 HybridCLR、YooAsset 和任意 HTTP/HTTPS CDN。CDN 可以是对象存储、云厂商 CDN、自建静态服务器、Nginx、OSS/COS/S3 兼容源站等，只要 YooAsset 生成的文件能通过 URL 访问即可。

## 单包结构

框架采用单 YooAsset 包结构：

- `DefaultPackage`：同时放 HybridCLR 热更 DLL、AOT 元数据 DLL 和普通热更新资源。

启动场景手动调用热更后，流程为整包下载：

1. 初始化 YooAsset。
2. 初始化 `DefaultPackage`，请求版本并更新清单。
3. 对 `DefaultPackage` 创建整包下载器并下载所有需要更新的文件。
4. 从 `DefaultPackage` 加载 AOT 元数据 DLL 和热更 DLL。
5. 将 `DefaultPackage` 设置为默认资源包。
6. 反射调用 `HotUpdate.HotUpdateEntry.Start`。

是否下载整个包由 `HotUpdateConfig.asset` 里的 `downloadPackage` 控制，默认开启。

## 运行时接入

框架不在 App 启动时自动开始热更，也不通过 `RuntimeInitializeOnLoadMethod` 自动创建对象。建议在 BootScene 里完成隐私协议、基础 SDK、网络检查、强更检查之后，再手动调用：

```csharp
HotUpdateConfig config = HotUpdateConfig.LoadDefault();
await HotUpdateService.Instance.RunAsync(config, progress, cancellationToken);
```

## 默认目录

项目内资源位置：

- 热更 DLL：`Assets/HotUpdateAssets/Assemblies/HotUpdate.dll.bytes`
- AOT 元数据 DLL：`Assets/HotUpdateAssets/Assemblies/AOT/*.dll.bytes`
- 普通热更新资源：`Assets/HotUpdateAssets/Resources`
- 热更新配置：`Assets/Resources/HotUpdateConfig.asset`

`HotUpdateConfig.asset` 里的程序集目录配置：

- `hotUpdateAssemblyAssetDirectory`：热更 DLL 的目标目录，默认是 `Assets/HotUpdateAssets/Assemblies`
- `aotMetadataAssetDirectory`：AOT 元数据 DLL 的目标目录，默认是 `Assets/HotUpdateAssets/Assemblies/AOT`

目录需要位于 `Assets` 下。程序集列表可以填写 `HotUpdate`、`HotUpdate.dll` 或 `HotUpdate.dll.bytes`，框架会转换为 YooAsset 使用的 `.dll.bytes` 资源路径。调整目录或程序集列表后，执行 `Hot Update/Prepare YooAsset DLL Assets` 和 `Hot Update/Build YooAsset Package`。

YooAsset Collector 默认配置：

- `DefaultPackage` 收集 `Assets/HotUpdateAssets`
- DLL 和 AOT 文件作为普通 `TextAsset` 打进 AssetBundle，并在运行时读取 `bytes`
- 资源定位使用完整资源路径，例如 `Assets/HotUpdateAssets/Assemblies/HotUpdate.dll.bytes`

`ProjectSettings/HybridCLRSettings.asset` 配置内容：

- 热更程序集：`HotUpdate`
- 默认 AOT 元数据程序集：`mscorlib.dll`、`System.dll`、`System.Core.dll`、`UnityEngine.CoreModule.dll`

## 编辑器流程

1. 如果项目尚未安装 HybridCLR，先执行 `HybridCLR/Installer...`。
2. 执行 `Hot Update/Generate HybridCLR/Compile HotUpdate DLLs` 编译热更 DLL。
3. 构建一次 Player，或执行 HybridCLR AOT 生成流程，确保裁剪后的 AOT DLL 已生成。
4. 执行 `Hot Update/Prepare YooAsset DLL Assets`，把 HybridCLR 产物复制到 `Assets/HotUpdateAssets/Assemblies`。
5. 在 YooAsset Collector 里配置 `DefaultPackage`，并收集 `Assets/HotUpdateAssets`。
6. 如果首包需要内置一份热更资源，勾选 `HotUpdateConfig.asset` 里的 `useBuildinFileSystemInHostMode`。
7. 执行 `Hot Update/Build YooAsset Package`，构建单个热更新包。
8. 开启内置文件时，重新构建 App 包，让 `Assets/StreamingAssets/DefaultPackage` 进入首包。
9. 将生成的 YooAsset 包目录发布到 CDN 源站。

`useBuildinFileSystemInHostMode` 关闭时，`HostPlayMode` 只使用远端 CDN 和本地缓存；开启时，构建菜单会使用 `ClearAndCopyAll` 把本次 YooAsset 构建结果拷到 `Assets/StreamingAssets/DefaultPackage`，运行时会先启用 Buildin 文件系统，再配合 CDN 检查更新。

## CDN 远程目录

默认 URL 模板是：

```text
{Root}/{Platform}/{PackageName}/{FileName}
```

如果 `HotUpdateConfig.asset` 里的 `RemoteMainRoot` 设置为：

```text
https://cdn.example.com/hotupdate
```

Android 平台会请求：

```text
https://cdn.example.com/hotupdate/Android/DefaultPackage/<YooAssetFileName>
```

所以 CDN 源站目录应该类似这样：

```text
<CDN源站根目录>/hotupdate/
  Android/
    DefaultPackage/
      <YooAsset输出文件>
  iOS/
    DefaultPackage/
      <YooAsset输出文件>
  Windows64/
    DefaultPackage/
      <YooAsset输出文件>
```

本地发布默认读取 `Tools/local_cdn_server.config.json`：

```powershell
python .\Tools\local_cdn_server.py
```

默认配置如下：

```json
{
  "BuildOutputRoot": "Bundles",
  "CdnRootDirectory": "LocalCdn",
  "Platform": "Android",
  "PackageName": "DefaultPackage",
  "CleanDestination": true,
  "StartLocalServer": false,
  "LocalServerHost": "0.0.0.0",
  "LocalServerPort": 8080,
  "LocalServerTestPath": "Android/DefaultPackage/DefaultPackage.version",
  "PauseOnExit": true
}
```

脚本会自动从 `Bundles/Android/DefaultPackage` 下寻找最新的 YooAsset 版本目录，并复制到：

```text
LocalCdn/Android/DefaultPackage
```

本地模拟 CDN 可以在配置里开启自动启动服务：

```json
{
  "StartLocalServer": true,
  "LocalServerHost": "0.0.0.0",
  "LocalServerPort": 8080
}
```

开启后执行发布脚本会直接启动 HTTP 服务，终端保持运行，按 `Ctrl+C` 停止。命令行可以临时开启：

```powershell
python .\Tools\local_cdn_server.py --start-local-server
```

此时 `HotUpdateConfig.asset` 里的 `RemoteMainRoot` 可以填：

```text
http://127.0.0.1:8080
```

发布到真实源站时，调整配置里的 `CdnRootDirectory`，例如：

```json
{
  "CdnRootDirectory": "D:/CdnOrigin/hotupdate"
}
```

然后 `RemoteMainRoot` 对应填你的公网地址，例如：

```text
https://cdn.example.com/hotupdate
```

命令行参数可临时覆盖配置：

```powershell
python .\Tools\local_cdn_server.py --platform iOS --cdn-root-directory "D:\CdnOrigin\hotupdate"
```

脚本结束后会等待回车关闭终端。命令行连续执行时可以关闭等待：

```powershell
python .\Tools\local_cdn_server.py --no-pause-on-exit
```

`Platform` 必须和 YooAsset 输出的平台目录完全一致，例如 `Android`，不要写成 `Andorid`。

脚本会复制到：

```text
<CdnRootDirectory>/<Platform>/<PackageName>
```

之后用你的 CDN/对象存储/服务器同步工具把 `CdnRootDirectory` 发布到公网。默认配置会清理目标目录，适合本地模拟；如果线上需要保留旧文件，可以把 `CleanDestination` 改成 `false`。

## Cloudflare R2 模拟

`Tools/sync_cdn_to_r2.py` 用 Python 将 `LocalCdn` 同步到 Cloudflare R2。密钥不写入项目配置，使用环境变量或 AWS Profile 提供。

安装 Python 依赖：

```powershell
python -m pip install -r .\Tools\requirements-r2.txt
```

配置 `Tools/r2_cdn_sync.config.json`：

```json
{
  "CdnRootDirectory": "LocalCdn",
  "BucketName": "your-r2-bucket",
  "AccountId": "your-cloudflare-account-id",
  "Prefix": "",
  "DeleteRemote": false,
  "PublishLocalFirst": true,
  "PublicRoot": "https://pub-xxxx.r2.dev",
  "InteractiveCredentials": true,
  "PauseOnExit": true
}
```

执行同步时，脚本会在当前终端提示输入 R2 S3 API 密钥：

```powershell
python .\Tools\sync_cdn_to_r2.py
```

也可以提前在终端设置环境变量，脚本检测到后不会再次询问：

```powershell
$env:AWS_ACCESS_KEY_ID="R2 Access Key ID"
$env:AWS_SECRET_ACCESS_KEY="R2 Secret Access Key"
python .\Tools\sync_cdn_to_r2.py
```

脚本会先按 `local_cdn_server.config.json` 刷新 `LocalCdn`，再同步到：

```text
s3://<BucketName>/<Prefix>
```

R2 公开访问地址对应填到 `HotUpdateConfig.asset`：

```text
RemoteMainRoot = https://pub-xxxx.r2.dev
RemoteFallbackRoot = https://pub-xxxx.r2.dev
```

如果 `Prefix` 设置为 `hotupdate`，远端文件路径为 `hotupdate/Android/DefaultPackage/...`，`RemoteMainRoot` 需要包含这个前缀：

```text
https://pub-xxxx.r2.dev/hotupdate
```

## 注意事项

- 真机联机更新建议使用 `HostPlayMode`，并确保源站目录结构和 URL 模板一致。
- `useBuildinFileSystemInHostMode` 只有在重新执行 `Hot Update/Build YooAsset Package` 并重新打 App 包后才对真机首包生效；只在运行时勾选但没有生成 `StreamingAssets/DefaultPackage`，会导致内置 catalog 或清单缺失。
- Editor 模拟模式依赖 `AssetBundleCollectorSetting.asset` 里存在 `DefaultPackage`。框架不在启动模拟构建前自动修改 Collector 配置。
- 业务资源默认放进 `Assets/HotUpdateAssets/Resources`，运行时可以通过 `YooAssets.GetPackage("DefaultPackage")` 或默认包加载。
