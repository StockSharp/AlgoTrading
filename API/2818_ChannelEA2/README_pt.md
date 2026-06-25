# Estratégia ChannelEA2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia ChannelEA2 replica o especialista MetaTrader "ChannelEA2" no StockSharp. A estratégia constrói um canal de preços intradiário entre as horas de início e fim de sessão configuradas. Quando a sessão termina, ela coloca ordens stop acima da máxima do canal e abaixo da mínima do canal. Cada ordem stop carrega um stop loss protetor definido pela borda oposta do canal. A abordagem visa capturar rompimentos após um período de consolidação durante a janela de sessão.

## Lógica de trading
- Na primeira vela finalizada cujo tempo de abertura cruza o `BeginHour`, a estratégia reinicia a sessão.
  - Todas as posições abertas são fechadas com ordens a mercado.
  - Quaisquer ordens ativas, incluindo entradas stop anteriores ou stops de proteção, são canceladas.
  - As máximas e mínimas da sessão são inicializadas usando a primeira vela dentro da nova sessão.
- Durante a sessão (de `BeginHour` até `EndHour`), a máxima e mínima de cada vela finalizada atualiza os limites do canal.
- Na primeira vela que abre após o término da sessão (`EndHour`), a estratégia calcula:
  - Uma ordem de compra stop na máxima de sessão registrada mais um buffer opcional medido em passos de preço.
  - Uma ordem de venda stop na mínima de sessão registrada menos o mesmo buffer.
  - O stop loss para a ordem de compra é a mínima da sessão, enquanto o stop loss para a ordem de venda é a máxima da sessão.
- Se uma posição for aberta, a ordem de entrada oposta é cancelada e um stop protetor é registrado no mercado usando o nível de stop armazenado.
- As ordens permanecem ativas até o início da próxima sessão, quando tudo é reiniciado novamente.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BeginHour` | Hora (0-23) quando a sessão é reiniciada e o canal começa a coletar dados. | `1` |
| `EndHour` | Hora (0-23) quando as ordens stop são programadas. Suporta sessões noturnas quando `BeginHour > EndHour`. | `10` |
| `TradeVolume` | Volume usado para cada ordem de entrada. | `1` |
| `CandleType` | Série de velas usada para construir o canal (padrão velas de 1 hora). | `1 hora` |
| `StopBufferMultiplier` | Multiplicador do passo de preço do instrumento usado como buffer de segurança para ativação de entrada e stops de proteção. | `2` |

## Gestão de risco
- A estratégia chama automaticamente `StartProtection()` para que o StockSharp gerencie posições inesperadas.
- As ordens stop de proteção são enviadas imediatamente após o aparecimento de uma posição. Elas são canceladas quando a posição retorna a zero.
- Os preços de stop são deslocados por `StopBufferMultiplier * PriceStep` para evitar violar os limites de distância de stop da bolsa.

## Notas adicionais
- O range do canal congela assim que as ordens stop são geradas; velas posteriores não afetam os níveis de entrada até que a próxima sessão comece.
- Se o instrumento não tiver `PriceStep` definido, o buffer é ignorado e as ordens são colocadas nos níveis exatos do canal.
- Os valores de volume são decimais, permitindo contratos ou lotes fracionários quando suportado pelo broker.
- A estratégia desenha velas e operações executadas na área do gráfico para acompanhamento visual.
