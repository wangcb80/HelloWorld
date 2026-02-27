# Win11 摘录式阅读软件设计方案（参考 MarginNote）

## 1. 产品定位

一款运行在 Windows 11 上的「阅读 + 摘录 + 关联思考」工具：

- 左侧阅读原文（PDF / DOCX 等）
- 右侧构建可拖拽的摘录卡片（摘录块）
- 卡片之间可建立永久可视化连接
- 支持从卡片回跳原文定位

目标用户：学生、研究者、知识工作者。

---

## 2. 核心功能设计（对应你的 4 条要求）

## 2.1 左侧打开书籍（PDF / DOCX 等）

### 支持格式（第一期）

- PDF（重点）
- DOCX
- TXT / Markdown
- （可选）EPUB

### 阅读器能力

- 分页/连续滚动两种模式
- 缩放、适应宽度、目录导航、页码跳转
- 文本选择 + 框选（区域截图式摘录）
- 文本搜索与高亮

### 推荐技术实现（Win11）

- 桌面框架：**Tauri + React + TypeScript**（轻量、跨平台）
- PDF 渲染：**PDF.js**
- DOCX 渲染：`mammoth.js`（提取结构）+ 自定义排版视图
- 本地文件访问：Tauri 文件系统 API

---

## 2.2 选中文字/框选后拖拽到右侧摘录块，并可回链

### 交互流程

1. 用户在左侧阅读区选择文本（或框选区域）。
2. 浮动工具条出现：`摘录`、`高亮`、`注释`。
3. 拖拽所选内容到右侧画布，自动生成摘录块。
4. 摘录块记录「来源锚点」（文档ID + 页码 + 坐标 + 文本上下文）。
5. 点击摘录块时：左侧自动打开对应文档并跳转到该位置，闪烁高亮。

### 数据结构建议

```ts
type SourceAnchor = {
  docId: string;
  page: number;
  rects?: Array<{ x: number; y: number; w: number; h: number }>;
  quote: string;           // 被摘录文本
  contextBefore?: string;  // 防止重排后定位失败
  contextAfter?: string;
};

type ExcerptCard = {
  id: string;
  boardId: string;
  contentType: 'text' | 'image' | 'mixed';
  content: string;
  source: SourceAnchor;
  x: number;
  y: number;
  w: number;
  h: number;
  tags: string[];
  createdAt: number;
  updatedAt: number;
};
```

### 回链定位策略（鲁棒）

- 一级定位：页码 + 坐标（最快）
- 二级定位：文本 quote 精确匹配
- 三级定位：contextBefore/contextAfter 模糊匹配
- 定位失败时给出“手动重新绑定”入口

---

## 2.3 摘录块边缘拖拽箭头连接，自动吸附，移动不断开

### 交互设计

- 鼠标悬停摘录块边缘，显示 4~8 个连接点（port）。
- 从连接点拖出“链接箭头”，靠近其他块边缘时自动吸附（snap）。
- 松开后生成连线（有方向）。
- 移动任一摘录块时，连线自动重算路径并保持连接。
- 选中连线按 `Delete` 即删除，不影响卡片。

### 连线类型

- 关系类型：`因果`、`并列`、`解释`、`反驳`、`例证`、`自定义`
- 视觉编码：颜色 + 线型（实线/虚线）+ 箭头样式
- 可在连线中部加标签（relationship label）

### 技术实现建议

- 画布引擎：React Flow / Konva / PixiJS（三选一）
- 路径算法：正交路径（Manhattan）+ 避障（第二期）
- 吸附逻辑：
  - 命中半径 `r = 12~18 px`
  - 最近 port 优先
  - 显示“吸附高亮”反馈

```ts
type Edge = {
  id: string;
  fromCardId: string;
  fromPort: 'top'|'right'|'bottom'|'left';
  toCardId: string;
  toPort: 'top'|'right'|'bottom'|'left';
  relation?: string;
  style?: { color: string; dashed: boolean };
};
```

---

## 2.4 参考 MarginNote 的扩展功能（建议实现）

以下按优先级拆分，避免一次做太重。

### P0（必须有）

1. **卡片编辑**：标题、正文、富文本、颜色、标签。
2. **双链导航**：
   - 卡片 -> 原文
   - 原文高亮 -> 对应卡片列表
3. **看板操作**：缩放、平移、框选多选、对齐分布。
4. **自动保存**：本地数据库实时保存，崩溃恢复。

### P1（强烈建议）

1. **学习模式（类似 MarginNote）**
   - 按标签/章节筛选卡片
   - 隐藏原文仅看卡片网络
2. **间隔复习（SRS）**
   - 卡片可设“记忆状态”
   - 每日复习队列
3. **大纲视图**
   - 从连接图自动生成层级大纲
4. **多文档工作区**
   - 同一主题下可关联多本书摘录

### P2（进阶）

1. AI 辅助：摘要、关键词、关系建议、问答。
2. OCR：扫描版 PDF 区域识别。
3. 云同步：OneDrive / WebDAV。
4. 导出：Markdown、OPML、Anki、XMind。

---

## 3. 信息架构与界面布局

## 3.1 主界面布局

- 顶部：项目/文档切换、搜索、同步状态、设置
- 左栏（Reader Pane）：文档阅读区
- 中间（可选）：迷你目录/文档结构树
- 右栏（Knowledge Board）：摘录块画布
- 底部：状态栏（页码、缩放、选中对象信息）

## 3.2 关键 UI 组件

- `DocumentTabs`：多文档标签页
- `ReaderViewport`：阅读渲染器
- `SelectionToolbar`：选区浮动工具条
- `BoardCanvas`：卡片画布
- `ExcerptCard`：摘录块组件
- `EdgeLayer`：连接线层
- `InspectorPanel`：属性面板

---

## 4. 技术架构（可落地）

## 4.1 客户端架构

- 前端：React + Zustand（状态管理）+ React Query
- 桌面壳：Tauri（Rust）
- 存储：SQLite（本地）
- 搜索：SQLite FTS5（全文检索）

## 4.2 模块划分

1. `file-service`：导入、解析、缓存
2. `reader-engine`：PDF/DOCX 渲染与定位
3. `excerpt-engine`：摘录创建、回链定位
4. `graph-engine`：卡片关系图与布局
5. `sync-service`（后续）：云端同步

## 4.3 数据表（简化）

- `documents(id, title, path, format, created_at)`
- `anchors(id, doc_id, page, rect_json, quote, ctx_before, ctx_after)`
- `cards(id, board_id, anchor_id, content, x, y, w, h, style_json, created_at)`
- `edges(id, from_card_id, to_card_id, relation, style_json, created_at)`
- `highlights(id, doc_id, anchor_id, color, note)`

---

## 5. 关键难点与解决方案

1. **DOCX 与 PDF 定位一致性问题**
   - 统一抽象成 `SourceAnchor`，尽量存储坐标 + 文本上下文双保险。
2. **大画布性能**
   - 视口裁剪（virtualization）+ 分层渲染（卡片层/线层）。
3. **拖拽连线体验**
   - 连接点磁吸 + 高亮反馈 + 撤销/重做。
4. **数据安全**
   - 本地自动备份 + 项目快照。

---

## 6. 版本路线图（建议）

### v0.1（4~6 周，MVP）

- 打开 PDF/DOCX
- 选区生成摘录块
- 点击摘录块回跳原文
- 卡片拖拽 + 基础连线 + 删除连线
- 本地自动保存

### v0.2（6~8 周）

- 标签系统、全文搜索
- 大纲视图、关系类型
- 多文档同屏
- 导出 Markdown

### v0.3（8~12 周）

- 复习模式（SRS）
- OCR 支持
- AI 摘要与问答
- 同步能力

---

## 7. 你可以直接给开发团队的需求摘要

> 请实现一个 Win11 桌面阅读软件：左侧阅读（PDF/DOCX），右侧知识画布。用户可以在左侧选中文本或框选区域，拖拽到右侧生成可编辑摘录块。摘录块要保存来源锚点，点击可精准跳回原文位置。摘录块边缘支持拖拽箭头连接，连接时自动吸附，移动卡片后连线保持不断开，支持选中连线删除。整体体验参考 MarginNote，逐步加入标签、大纲、复习、导出与 AI 辅助功能。 

---

如果你愿意，我下一步可以继续给你：

1. **高保真交互原型清单（每个页面每个按钮）**
2. **数据库完整 schema（可直接建表）**
3. **MVP 的开发任务拆解（按周排期 + 人员分工）**
