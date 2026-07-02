# Estratégia de oscilação do regime Rabbit M2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Rabbit M2 é um consultor especialista discricionário originalmente codificado por Peter Byrom para MetaTrader 4. O algoritmo alterna entre
regimes de alta e baixa determinados por médias móveis exponenciais horárias. Dentro do regime ativo ele escuta Williams %R
oscilações de impulso que são confirmadas pelo Commodity Channel Index antes de enviar ordens de mercado. A lógica protetora reflete a
fonte EA anexando níveis de stop loss e takeprofit de distância fixa e fechando posições sempre que o preço violar o
oposto ao limite do canal Donchian. Um módulo simples de gerenciamento de dinheiro aumenta o tamanho do lote base após cada operação altamente lucrativa.
comércio e dobra a meta de lucro necessária para a próxima expansão.

## Dados e indicadores de mercado
- **Período principal** (padrão: velas de 1 minuto) fornece entradas para Williams %R, CCI e o canal Donchian.
- **O período de hora em hora** calcula o par rápido (40) e lento (80) EMA que controla a direção da negociação.
- **Williams %R (50)** atua como gatilho de impulso quando cruza as bandas -20/-80.
- **Commodity Channel Index (14)** filtra as negociações exigindo leituras de sobrecompra ou sobrevenda.
- **Donchian Canal (100)** fornece saídas de breakout com base na faixa máxima/baixa anterior.
- **Stop Loss e Static Take Profit** são convertidos de distâncias de pontos (padrão 50) em compensações de preço usando o tick de segurança
tamanho, ajustado para 3 e 5 instrumentos decimais.

## Lógica de negociação
### Gestão do regime
1. Quando o período de 40 EMA no feed horário cai abaixo do período de 80 EMA, todas as posições longas são fechadas e apenas as configurações curtas
are allowed.
2. Quando o período de 40 EMA sobe acima do período de 80 EMA, as posições vendidas são liquidadas e a estratégia permite apenas negociações longas.

### Regras de entrada
- **Short entries** require:
  - Williams %R para passar da zona -20..0 para o território de sobrevenda (< -20).
  - CCI para exceder o limite de venda configurável (padrão 101).
  - Exposição líquida curta abaixo do limite `MaxTrades` (cada negociação adiciona uma unidade de volume base).
- **Entradas longas** exigem:
  - Williams %R para sair da zona -100..-80 e imprimir um valor acima de -80.
  - CCI fique abaixo do limite de compra (padrão 99).
  - Exposição longa líquida abaixo do limite de `MaxTrades`.

Cada pedido é enviado com o volume base atual. A porta StockSharp usa posições de compensação, então a repetição de sinais simplesmente aumenta
a exposição líquida até que o limite configurado seja atingido.

### Regras de saída
1. Os níveis de stop loss e takeprofit são monitorados em cada vela finalizada. Quando o preço ultrapassa um nível, a posição é
fechado com uma ordem de mercado.
2. Independentemente dos níveis de stop/alvo, uma posição longa é fechada quando o fechamento cai abaixo da banda inferior anterior Donchian;
uma venda é fechada quando o fechamento sobe acima da banda superior anterior Donchian.
3. Uma mudança de regime causada pelo cruzamento horário EMA liquida imediatamente as posições que se opõem à nova direção.

### Gestão de dinheiro
- O tamanho base do pedido começa em `InitialVolume` (padrão 0,01) e respeita a etapa do volume de segurança, mínimo e máximo.
- Após cada lucro realizado superior a `BigWinTarget` (padrão 15 unidades monetárias), o volume base aumenta em
`VolumeIncrement` (padrão 0,01) e o limite de lucro dobra, correspondendo ao comportamento em cascata da versão MetaTrader.
- Quando a estratégia é plana, quaisquer espaços reservados de stop/take pendentes são redefinidos para evitar valores obsoletos.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CciSellLevel` | 101 | Valor mínimo CCI que confirma um sinal curto. |
| `CciBuyLevel` | 99 | Valor máximo CCI que confirma um sinal longo. |
| `CciPeriod` | 14 | Comprimento de lookback do índice de canal de commodities. |
| `DonchianPeriod` | 100 | Donchian período do canal usado para saídas de breakout. |
| `MaxTrades` | 1 | Número máximo de unidades de volume base permitidas na posição líquida. |
| `BigWinTarget` | 15 | Lucro realizado necessário antes de aumentar o volume base. |
| `VolumeIncrement` | 0,01 | Volume adicional adicionado após uma vitória na qualificação. |
| `WprPeriod` | 50 | Williams %R período de cálculo. |
| `FastEmaPeriod` | 40 | Período EMA rápido no feed de tendência horária. |
| `SlowEmaPeriod` | 80 | Período EMA lento no feed de tendência horária. |
| `TakeProfitPoints` | 50 | Tire a distância do lucro em faixas de preço. |
| `StopLossPoints` | 50 | Pare a distância de perda em faixas de preço. |
| `InitialVolume` | 0,01 | Tamanho inicial do pedido base. |
| `CandleType` | Velas de 1 minuto | Período primário usado para cálculos de impulso e saída. |

## Notas de implementação
- Os níveis de stop loss e takeprofit são avaliados dentro da estratégia, em vez de serem enviados como ordens separadas para replicar o
comportamento dos parâmetros `OrderSend` de MetaTrader.
- Os ajustes de volume dependem do PnL realizado relatado por StockSharp. Certifique-se de que a estratégia receba confirmações de negociação do
conexão do corretor para que a lógica de escalabilidade seja ativada.
- O método auxiliar `CalculatePriceOffset` aumenta o tamanho do ponto para símbolos forex de 3 e 5 decimais, reproduzindo o `Point`
constante da plataforma original.
