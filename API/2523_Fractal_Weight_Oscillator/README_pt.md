# Estratégia Oscilador de Peso Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia replica o assessor especialista "Exp_Fractal_WeightOscillator" agregando quatro osciladores (RSI, Money Flow Index, Williams %R e DeMarker) em um único sinal composto suavizado. O oscilador é comparado com dois níveis horizontais (`HighLevel`/`LowLevel`) para acionar negociações compradas ou vendidas no modo de seguimento de tendência ou contratendência. Todos os cálculos são realizados no período de velas selecionado e usam a API de alto nível padrão do StockSharp.

## Pilha de indicadores
- **Índice de Força Relativa** – aplicado à fonte de preço configurada.
- **Money Flow Index** – calculado a partir do preço aplicado escolhido e do volume de velas.
- **Williams %R** – calculado a partir dos valores de máximo/mínimo/fechamento da vela.
- **DeMarker** – recriado a partir das máximas e mínimas das velas com um suavizador de média simples.
- **Suavizador de média móvel** – pós-processamento opcional da soma ponderada (SMA, EMA, SMMA ou LWMA).

O valor do oscilador composto é uma média ponderada dos quatro componentes. `HighLevel` e `LowLevel` definem zonas de sobrecompra/sobrevenda. `SignalBar` controla quantas barras completadas são inspecionadas ao procurar um cruzamento para que você possa atrasar a execução em relação à vela mais recente terminada.

## Lógica de trading
### TrendMode = Direct
- **Entrada comprada / saída vendida** – quando o oscilador cai de acima de `LowLevel` para abaixo ou igual a `LowLevel` (`BuyOpenEnabled` e `SellCloseEnabled` devem ser verdadeiros).
- **Entrada vendida / saída comprada** – quando o oscilador sobe de abaixo de `HighLevel` para acima ou igual a `HighLevel` (`SellOpenEnabled` e `BuyCloseEnabled` devem ser verdadeiros).

### TrendMode = Counter
- **Entrada comprada / saída vendida** – acionada por um rompimento para cima de `HighLevel`.
- **Entrada vendida / saída comprada** – acionada por um rompimento para baixo de `LowLevel`.

Os sinais são avaliados na barra especificada por `SignalBar`. As reversões de posição usam `Volume + |Posição|` para neutralizar qualquer exposição existente.

## Gestão de risco
Quando uma nova posição é aberta, a estratégia calcula níveis fixos de preço de stop-loss e take profit usando `StopLossPoints` e `TakeProfitPoints`. Os valores são multiplicados pelo `MinPriceStep` do instrumento. Em cada vela completada, o mínimo/máximo é verificado contra esses alvos; se atingidos, a posição é fechada imediatamente e os rastreadores internos de risco são redefinidos.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| `TrendMode` | Selecionar comportamento direto (seguimento de tendência) ou contratendência. |
| `SignalBar` | Número de barras fechadas para trás usadas para avaliação de sinal. |
| `Period` | Comprimento base para RSI, MFI, Williams %R e DeMarker. |
| `SmoothingLength` | Janela para o suavizador de média móvel. |
| `SmoothingMethod` | Tipo de média móvel (`None`, `Sma`, `Ema`, `Smma`, `Lwma`). |
| `RsiPrice`, `MfiPrice` | Fonte de preço aplicada usada nos osciladores componentes. |
| `MfiVolume` | Tipo de volume para MFI (tick e real ambos usam volume de velas). |
| `RsiWeight`, `MfiWeight`, `WprWeight`, `DeMarkerWeight` | Pesos relativos no oscilador composto. |
| `HighLevel`, `LowLevel` | Limiares superior e inferior para cruzamentos de nível. |
| `BuyOpenEnabled`, `SellOpenEnabled` | Habilitar entradas compradas ou vendidas. |
| `BuyCloseEnabled`, `SellCloseEnabled` | Permitir fechar posições existentes em sinais opostos. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção em passos de preço (0 desabilita o nível). |
| `CandleType` | Período das velas passadas à estratégia. |
| `Volume` *(Propriedade de estratégia)* | Tamanho da negociação usado para entradas (as reversões de posição adicionam a posição absoluta). |

## Notas de uso
- `SignalBar = 1` reproduz o comportamento do especialista original usando a última barra completamente fechada. Aumentar o valor atrasa as reações por barras adicionais.
- `SmoothingMethod` permite desativar a suavização (`None`) ou corresponder aos diferentes estilos de média móvel disponíveis na versão MQL.
- A implementação do Money Flow Index sempre trabalha com o volume total da vela fornecido pelo feed de dados. Portanto, ambas as opções `Tick` e `Real` se referem ao mesmo valor agregado porque as velas do StockSharp não expõem contadores de tick separados por padrão.
- Todos os comentários no fonte C# estão escritos em inglês, conforme necessário.
