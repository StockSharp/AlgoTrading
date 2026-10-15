# FrBestExp02 Estratégia Mod Maloma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão C# do MetaTrader 4 expert `Frbestexp02_1_maloma_mod.mq4`. Ele combina impulso OsMA, reversões fractais, confirmação de volume de ticks e um filtro de pivô diário contínuo para atenuar movimentos exaustos no período M15.

## Lógica de negociação

- **Pivot da sessão** – um ponto de pivô rotativo é calculado a partir da máxima mais alta, da mínima mais baixa e do fechamento mais antigo dentro de uma janela configurável (96 velas por padrão, igual a um dia de negociação no M15). Somente negociações que concordam com o viés do pivô são permitidas: vendas acima do pivô e posições compradas abaixo dele.
- **Padrão fractal** – a estratégia aguarda por um fractal confirmado de Bill Williams três velas atrás. Fractais descendentes (mínimos oscilantes) permitem vendas, enquanto fractais ascendentes (máximos oscilantes) permitem posições compradas.
- **Histograma OsMA** – um histograma MACD (rápido 12, lento 26, sinal 9 por padrão) deve estar mais inclinado para o território negativo para posições vendidas e mais para o território positivo para posições longas. A leitura anterior do histograma também deve estar do mesmo lado de zero.
- **Filtro de volume** – o volume da vela finalizada anterior deve exceder um limite configurável e ser maior que o volume de duas velas atrás. Isso reproduz o requisito de pico de volume de ticks do especialista original.
- **Tempo do pedido** – as negociações são aceleradas por um intervalo mínimo (20 segundos por padrão) entre as entradas.
- **Gestão de risco** – stop-loss configurável, take-profit e trailing stop opcional são expressos em pontos e convertidos em preços de instrumentos. As ordens de proteção são atualizadas com os auxiliares `SetStopLoss`/`SetTakeProfit` integrados.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Volume de pedidos usado para cada entrada. | 1 |
| `StopLossPoints` | Distância de stop-loss em pontos do instrumento. | 1000 |
| `TakeProfitPoints` | Distância de lucro em pontos de instrumento. | 1000 |
| `TrailingStopPoints` | Distância de parada opcional em pontos (0 desativa o rastreamento). | 0 |
| `VolumeThreshold` | Volume mínimo de vela anterior necessário para ativar um sinal. | 50 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | Parâmetros MACD usados para calcular o histograma OsMA. | 26/12/9 |
| `PivotWindow` | Número de velas finalizadas incluídas no cálculo do pivô. | 96 |
| `MinTradeIntervalSeconds` | Número mínimo de segundos entre novas entradas. | 20 |
| `CandleType` | Período primário (o padrão é velas de 15 minutos). | M15 |

## Diferenças versus o especialista MQL4

- O código original suportava ordens de hedge multiplicadas por `kh` e lógica complexa de reciclagem de lucros. A versão StockSharp executa uma única posição direcional e a fecha ou reverte antes de abrir uma nova negociação.
- O tratamento do trailing stop é simplificado para usar o auxiliar `SetStopLoss` padrão em vez de modificar manualmente os pedidos por tick.
- A agregação de lucros e os blocos de recuperação do tipo martingale são omitidos. O gerenciamento de saída depende de stop-loss, take-profit ou trailing stop.
- Todos os cálculos dos indicadores são orientados por eventos em velas finalizadas. Não há modificação de ordem intrabar.

## Notas de uso

1. Anexe a estratégia a um instrumento que forneça dados de volume de ticks se o filtro de volume corresponder ao comportamento original.
2. Mantenha o prazo de 15 minutos para reproduzir a calibração original da janela dinâmica e do lookback fractal.
3. Ajuste os períodos `VolumeThreshold` e OsMA para ajustar símbolos com diferentes perfis de volatilidade ou volume.
4. Habilite o trailing stop somente quando for desejada uma saída mais fechada; caso contrário, deixe-o em zero para confiar na parada/alvo estático.

O código segue as diretrizes de alto nível StockSharp API: assinaturas de velas via `SubscribeCandles`, vinculação de indicador para o histograma MACD e execução segura por meio de `BuyMarket`/`SellMarket` com ordens de proteção automáticas.
