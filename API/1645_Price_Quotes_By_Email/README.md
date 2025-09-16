# Price Quotes By Email Strategy

This strategy periodically sends account information and price quotes for selected symbols via email. The message contains the current bid price and the percentage change from the previous day's close for each instrument.

## Parameters

- **Send Interval (min)**: Frequency of email delivery in minutes. Setting zero disables sending.
- **Symbols (comma separated)**: List of instruments to include in the email.
- **Email From**: Sender email address.
- **Email To**: Recipient email address.
- **SMTP Host**: SMTP server address.
- **SMTP Port**: SMTP server port.
- **SMTP User**: SMTP username.
- **SMTP Password**: SMTP password.

## Notes

- The strategy must run on an active connection to receive fresh Level1 data.
- SMTP settings should be configured with valid credentials.
- No trading actions are performed; this example demonstrates data access and notification capabilities.
