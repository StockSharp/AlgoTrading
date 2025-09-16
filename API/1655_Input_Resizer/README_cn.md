# 输入窗口调整工具

该工具策略为活动对话框添加可调整大小的边框，并可选择记住窗口尺寸。它移植自 MQL4 脚本 **InputResizer**。

## 参数
- `Remember Size` – 保存窗口大小。
- `Individual` – 按窗口标题分别保存尺寸。
- `Init Maximized` – 首次运行时最大化窗口。
- `Init Custom` – 首次运行时使用自定义尺寸。
- `Init X`, `Init Y` – 在 `Init Custom` 启用时的初始位置。
- `Init Width`, `Init Height` – 在 `Init Custom` 启用时的初始宽高。
- `Sleep Time` – 检查窗口的延迟（毫秒）。
- `Weekend Mode` – 即使没有市场数据也继续运行。

## 逻辑
策略启动后会在后台循环中监视当前前台窗口。当发现新的对话框时，它会添加可调整大小的边框并应用启动设置。如果开启记忆功能，会记录窗口的最后尺寸，并在下次打开同一窗口时恢复。

该策略不进行交易，可与其他策略同时使用。
