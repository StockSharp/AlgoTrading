# Estratégia de E-mail de Status e Alerta no Fechamento de Ordem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora a conta e relata eventos importantes:

- Envia uma notificação de status diária em um minuto especificado.
- Informa sobre cada ordem fechada com informações básicas da operação.

É baseada no especialista MQL *StatusMailandAlertOnOrderClose.mq4* e demonstra como lidar com notificações no StockSharp.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `SendReportEmail` | Habilitar notificação de status diária. |
| `StatusEmailMinute` | Minuto da hora para enviar a mensagem de status. |
| `SendClosedEmail` | Habilitar notificações quando ordens são fechadas. |
| `StartBalance` | Saldo inicial da conta usado para cálculo de lucro. |
| `CandleType` | Período usado para verificar o horário. Normalmente definido como 1 minuto. |

## Lógica

1. Assinar velas do período escolhido.
2. Quando uma vela termina, verificar se é o minuto especificado e enviar uma mensagem de relatório.
3. A cada nova operação, notificar se uma ordem foi fechada.

Essas mensagens são registradas via `AddInfo`, mas podem ser substituídas por qualquer mecanismo de notificação desejado.
