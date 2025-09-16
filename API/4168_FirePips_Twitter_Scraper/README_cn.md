# FirePips Twitter Scraper Strategy

## 概述
**FirePips Twitter Scraper Strategy** 是 MetaTrader 脚本 `firepips_.mq4` 的完整 StockSharp 版本。原始 MQ4 文件并不是一
个交易机器人，而是一个基于 WinInet 的小工具，会下载 FirePips 信号服务的公开 Twitter 页面，在 HTML 中查找特定
订单编号的出现次数，然后把获取到的原始响应保存到磁盘供人工检查。这个 C# 实现把同样的数据获取流程移植到
StockSharp 中，从而可以在平台的日志系统内自动化执行、集中监控，并在需要时与其他基础设施组件组合使用。

与典型的交易策略不同，这个移植版本不会订阅行情数据，也不会下单。它专注于可靠的 HTTP 通信、确定性的文本
处理以及与 MQL 脚本 `Alert` 和 `Print` 语句等价的详尽遥测。用户可以在 Designer、Shell 或 Runner 中启动该策略，
按需定期抓取 FirePips 网页，并核对默认的目标标识（`"Order ID: 7"`）。

## 执行流程
1. **启动** —— `OnStarted` 会立即调度一个异步后台任务，避免在 HTTP 请求期间阻塞 UI 线程。状态信息通过
   `AddInfoLog` 写入日志，对应 MetaTrader 中的 `Alert` 弹窗。
2. **HTTP 下载** —— 策略使用自定义超时创建 `HttpClient` 并下载目标 URL。出现非成功状态码或传输异常时，会捕获
   异常并调用 `AddErrorLog`，与原始脚本中的 `Alert` 错误分支保持一致。
3. **内容校验** —— 若响应体为空，则输出警告并提前结束流程，重现 MQ4 脚本中的 `Alert("Nicht nur ein paar daten")`
   消息。
4. **搜索循环** —— 下载的字符串按照 MQL `StringFind` 的迭代方式进行扫描：定位所有匹配子串并记录零基字符索引，
   与脚本中 `Print("order 7 found atz=", index)` 的输出等价。
5. **文件保存** —— 使用 UTF-8 编码把响应写入用户配置的文件名，等价于原实现中的
   `FileOpen("SavedFromInternet.htm", FILE_CSV|FILE_WRITE)` 和 `FileWrite` 组合。
6. **标记提取** —— 策略会检查分号 `;` 之前的第一个标记，模拟原脚本试图从保存的文件中读取订单类型的 CSV 分
   析逻辑。若能成功转换为整数则记录结果，否则会输出说明未检测到数字标记的原因。
7. **终止** —— 无论任务完成或出错，都会调用 `Stop()`，触发 `OnStopped` 并写入最终完成消息。

## 参数
| 名称 | 类型 | 默认值 | 描述 |
| --- | --- | --- | --- |
| `RequestUrl` | `string` | `http://twitter.com/FirePips` | 要下载的远程地址，对应 MQL 脚本中硬编码的 WinInet 目标。 |
| `SearchText` | `string` | `Order ID: 7` | 在 HTML 中查找的子串，每发现一次就写入一条信息日志。 |
| `OutputFileName` | `string` | `SavedFromInternet.htm` | 保存响应的本地文件名，相对路径以当前工作目录为基准。 |
| `RequestTimeout` | `int` | `30000` | HTTP 超时时间（毫秒），控制等待慢速请求的最长时间。 |

所有参数都通过 `StrategyParam<T>` 暴露，可以在 StockSharp UI 中调整，也可以用于优化不同组合，例如监控多个标识
或备选 URL。

## 与 MQL 脚本的对应关系
- **WinInet 与 HttpClient** —— `InternetOpenA`、`InternetOpenUrlA` 和 `InternetReadFile` 被 `HttpClient` 调用取代，并配
  合 `using` 块处理资源释放。
- **循环控制** —— MQ4 脚本在 `while` 循环中反复检查 `IsStopped()`；StockSharp 版本依赖框架的协作式终止机制，在
  下载结束后调用 `Stop()`，不再需要忙等。
- **字符串搜索** —— 顺序调用 `StringFind` 的逻辑由 `FindOccurrences` 完成，保持大小写敏感且不重叠的匹配语义。
- **文件处理** —— 不再使用多个 `FileOpen` 模式模拟 CSV 行为，而是通过 `File.WriteAllText` 一次性写入全部内容，
  随后的标记提取保留了尝试解析首个整数值的思路。
- **告警与诊断** —— 所有 `Alert` 分支都映射为清晰的日志条目：错误通过 `AddErrorLog` 报告，警告使用
  `AddWarningLog`，普通进度信息使用 `AddInfoLog`。

## 使用建议
- 如果需要周期性下载，可在 StockSharp Runner 中调度该策略，Runner 可以在固定间隔重新启动策略并复用同一套参
  数。
- 调整 `RequestUrl` 和 `SearchText` 以监控其他 FirePips 订单编号，或者用于完全不同的服务；只要响应是文本数据，
  逻辑就可以复用。
- 投入生产使用时，可以把 `OutputFileName` 指向专用目录（例如 `%APPDATA%/FirePips` 或 `/var/stocksharp/firepips`），
  便于管理快照文件。
- 可将此爬取策略与第二个 StockSharp 策略组合，后者读取保存的文件并对新订单编号作出反应，从而把 FirePips 信
  号纳入自动化流程。
- 使用 StockSharp 的日志查看器审查信息消息，确认搜索循环找到了期望的出现位置。

## 与原实现的差异
- 新版本采用异步、非阻塞流程，而 MQ4 脚本依赖 `Sleep(1)` 的阻塞循环。
- 文件默认使用 UTF-8 编码，避免 MetaTrader `FileWrite` 默认 ANSI 编码导致的地区性问题。
- 网络错误会输出详细异常信息，而不是简化的 "Error with InternetOpenUrlA()" 提示，有助于在代理或防火墙导致故障
  时排障。
- 下载完成后策略会自动调用 `Stop()` 表示任务结束，用户无需像在 MetaTrader 中那样手动关闭脚本。
