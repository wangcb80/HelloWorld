# Win11 免安装版摘录阅读软件设计（非 HTML 交付形态）

## 1. 目标与交付形态

你当前的诉求是：**不要浏览器 HTML 原型，而是 Win11 可直接运行、免安装的软件**。

推荐交付形态：

- `ReaderNote.exe`（主程序）
- `data/`（SQLite 数据库、日志、缓存）
- `resources/`（图标、默认模板）

用户下载后解压即可双击运行，无需安装、无需管理员权限。

---

## 2. 技术路线（桌面原生优先）

## 2.1 推荐方案：.NET 8 + WinUI 3（Windows App SDK）

理由：

- 原生 Windows 11 UI 体验更一致
- 对高 DPI、窗口管理、多显示器支持成熟
- 可通过自包含发布（self-contained）做到免安装
- 后续可扩展到 OCR、AI、插件机制

## 2.2 不推荐继续作为纯 HTML 交付

- 浏览器页签运行不具备桌面应用入口与文件关联能力
- 本地文件权限与系统集成受限
- 用户心智上仍认为是“网页工具”，不符合“桌面软件”预期

---

## 3. 软件架构设计

## 3.1 分层

1. **UI 层（WinUI 3）**
   - 左侧阅读器（PDF / DOCX）
   - 右侧摘录画布（卡片+连线）
   - 顶部命令栏（导入、保存、导出）

2. **Domain 层**
   - `DocumentAnchor`：来源锚点
   - `ExcerptCard`：摘录卡片
   - `EdgeLink`：卡片连线
   - 回链定位与重定位策略

3. **Data 层**
   - SQLite（WAL 模式）
   - 自动保存（防崩溃）
   - FTS 全文检索

4. **Infra 层**
   - PDF 渲染（PDFium）
   - DOCX 解析（OpenXML SDK）
   - 日志/异常上报（本地）

---

## 4. 核心功能落地（对应你的需求）

## 4.1 左侧打开文档（PDF / DOCX）

- PDF：页面渲染 + 文本坐标映射
- DOCX：段落、标题、批注、页内定位（逻辑定位）
- 支持拖拽文件到窗口直接导入

## 4.2 选中内容 -> 拖到右侧生成摘录块

- 文本选区触发浮动操作条：摘录/高亮/注释
- 拖入右侧画布创建卡片并保存锚点
- 锚点字段：`docId + page + rect + quote + context`

## 4.3 摘录块连线吸附、移动不断开

- 卡片四边连接点（port）
- 拖拽建立连线，目标边缘高亮吸附
- 连线依附卡片坐标实时重算
- 选中连线按 Delete 删除

## 4.4 卡片回跳原文

- 单击卡片“回到原文”
- 左侧自动打开对应文档与页码
- 高亮定位区并短暂闪烁

---

## 5. 数据模型（SQLite）

```sql
CREATE TABLE documents (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL,
  path TEXT NOT NULL,
  format TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE anchors (
  id TEXT PRIMARY KEY,
  doc_id TEXT NOT NULL,
  page INTEGER,
  rect_json TEXT,
  quote TEXT,
  ctx_before TEXT,
  ctx_after TEXT,
  FOREIGN KEY(doc_id) REFERENCES documents(id)
);

CREATE TABLE cards (
  id TEXT PRIMARY KEY,
  anchor_id TEXT,
  title TEXT,
  content TEXT,
  x REAL NOT NULL,
  y REAL NOT NULL,
  w REAL NOT NULL,
  h REAL NOT NULL,
  style_json TEXT,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  FOREIGN KEY(anchor_id) REFERENCES anchors(id)
);

CREATE TABLE edges (
  id TEXT PRIMARY KEY,
  from_card_id TEXT NOT NULL,
  to_card_id TEXT NOT NULL,
  relation TEXT,
  style_json TEXT,
  created_at INTEGER NOT NULL,
  FOREIGN KEY(from_card_id) REFERENCES cards(id),
  FOREIGN KEY(to_card_id) REFERENCES cards(id)
);
```

---

## 6. 免安装发布方案（关键）

## 6.1 发布命令（示意）

```powershell
dotnet publish .\ReaderNote\ReaderNote.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true
```

产物：单个 exe（可配合 `data` 目录）。

## 6.2 便携模式（Portable）

- 程序启动优先读取 `./data/app.db`
- 若不存在则自动创建
- 所有用户数据只写当前目录，不写注册表
- 退出时自动 flush + checkpoint

## 6.3 自动更新（可选）

- 免安装版建议先不做强制更新
- 使用“检查更新 -> 下载新压缩包 -> 用户替换目录”策略

---

## 7. MVP 开发排期（4 周）

### 第 1 周

- WinUI 主框架 + 左右分栏
- PDF 打开与基础阅读

### 第 2 周

- 文本选区摘录 -> 右侧卡片
- 卡片拖拽与编辑

### 第 3 周

- 连线吸附 + 删除
- 卡片回跳原文

### 第 4 周

- SQLite 自动保存
- 导入导出（JSON）
- 打包成免安装 exe

---

## 8. 与当前 HTML 原型的关系

- 当前 HTML 原型仅用于交互验证
- 后续以本设计文档作为“桌面化重构基线”
- 业务概念（卡片/锚点/连线）保留，UI 与运行时迁移到 WinUI

