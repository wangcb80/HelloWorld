# HelloWorld

这个仓库已从 HTML 原型方向转向 **Win11 免安装桌面软件**。

## 目录说明

- `ReaderNote/`：WinUI 3 桌面工程骨架（后续主线）
- `WIN11_PORTABLE_APP_DESIGN.md`：免安装版架构与发布设计
- `DESIGN_WIN11_READER.md`：完整产品需求与交互设计
- `app.html`：早期交互验证原型（仅参考，不再作为最终交付）

## Win11 本地构建（在 Windows 环境执行）

```powershell
cd .\ReaderNote
dotnet restore
dotnet build -c Release
```

## 发布免安装单文件（在 Windows 环境执行）

```powershell
cd .\ReaderNote
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

输出位于：`ReaderNote\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\`
