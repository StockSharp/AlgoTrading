# MT4 Mail Status Strategy

## Overview

The **MT4 Mail Status Strategy** periodically sends an email containing information about all active orders. It was converted from the original MQL script `MT4MailStatus.mq4` and adapted for the StockSharp API.

This strategy does not open or manage trades. Instead, it monitors existing orders and delivers a detailed status report at a configurable interval. If sending the email fails, the strategy writes the error message to a local log file.

## Parameters

- **SendInterval** – time between status reports. The minimum value is 60 seconds. Default: 1 hour.
- **SmtpHost** – SMTP server address.
- **SmtpPort** – SMTP server port.
- **SmtpUser** – user name for SMTP authentication.
- **SmtpPassword** – password for SMTP authentication.
- **FromEmail** – sender email address.
- **ToEmail** – recipient email address.

## How It Works

1. After the strategy starts, a timer checks every two seconds whether enough time has passed since the last report.
2. When the interval is reached and there are active orders, it constructs an email message listing:
   - Order identifier and symbol
   - Order type and price
   - Stop-loss and take-profit levels
   - Current best ask and bid
   - Order volume
3. The message is sent using the configured SMTP settings. Any failure is logged to a text file named `mylog_YYYY_MM_DD.txt`.

## Usage Notes

- The strategy assumes that the SMTP server supports SSL.
- No trades are created or modified. Only existing orders related to the strategy's portfolio are reported.
- Ensure that the SMTP credentials and the email addresses are correct to avoid delivery failures.

## Files

- `CS/Mt4MailStatusStrategy.cs` – main C# implementation.
- `README.md` – this documentation in English.
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.
