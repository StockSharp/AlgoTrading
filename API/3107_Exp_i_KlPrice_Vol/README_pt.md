# Estratégia Exp i-KlPrice Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é uma conversão em C# do especialista MetaTrader **Exp_i-KlPrice_Vol.mq5**. Reconstrói o oscilador KlPrice
que mede a distância entre o preço e uma banda de volatilidade, multiplica o oscilador pelo volume da candle e rastreia
transições de cor geradas por limiares adaptativos. Dois slots de posição independentes são emulados para cada direção,
espelhando o comportamento dual-magic do consultor especialista original.

## Lógica do Indicador
- O preço é transformado usando o modo `AppliedPrice` selecionado (close, open, median, Demark, etc.).
- O preço transformado é suavizado pelo método de média móvel definido em `PriceMaMethod` e `PriceMaLength`.
- O intervalo da candle (`High - Low`) é suavizado com `RangeMaMethod`/`RangeMaLength`. O intervalo age como largura dinâmica
  de banda.
- O oscilador KlPrice base é calculado como `100 * (Price - (MA - RangeMA)) / (2 * RangeMA) - 50`.
- O oscilador é multiplicado pela fonte de volume selecionada (`AppliedVolume.Tick` ou `AppliedVolume.Real`).
- Um suavizador Jurik de comprimento `SmoothingLength` é aplicado tanto ao oscilador quanto ao volume bruto, criando duas
  séries adaptativas.
- Limiares adaptativos são obtidos multiplicando o volume suavizado por `HighLevel2`, `HighLevel1`, `LowLevel1` e `LowLevel2`.
- A cor atual do oscilador é determinada comparando o valor do oscilador suavizado com os limiares adaptativos:
  - **4** – acima de `HighLevel2 * volume` (pressão altista extrema).
  - **3** – entre `HighLevel1 * volume` e o nível extremo.
  - **2** – entre os limiares altista e baixista.
  - **1** – entre o limiar inferior e a linha neutra.
  - **0** – abaixo de `LowLevel2 * volume` (pressão baixista extrema).

## Regras de Trading
1. Avaliar a cor em `SignalBar` (geralmente a candle completada anterior) e a cor antes dela.
2. Entradas compradas:
   - O Slot 1 abre quando a cor muda de **4** para qualquer valor abaixo de **4** e `AllowLongEntry` é `true`.
   - O Slot 2 abre quando a cor muda de **3** para abaixo de **3**.
3. Entradas vendidas:
   - O Slot 1 abre quando a cor sobe de **0** para acima de **0** e `AllowShortEntry` é `true`.
   - O Slot 2 abre quando a cor sobe de **1** para acima de **1**.
4. Saídas compradas ocorrem quando a cor anterior era **0** ou **1** e `AllowLongExit` está habilitado.
5. Saídas vendidas ocorrem quando a cor anterior era **4** ou **3** e `AllowShortExit` está habilitado.
6. Cada slot rastreia o tempo do último sinal para evitar ordens duplicadas na mesma candle. Stops de proteção são opcionais
   e são tratados através de `StartProtection` quando `StopLossPoints` ou `TakeProfitPoints` são maiores que zero.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|--------|-----------|
| `PrimaryVolume` | `decimal` | `0.1` | Volume usado pelo primeiro slot comprado/vendido. |
| `SecondaryVolume` | `decimal` | `0.2` | Volume usado pelo segundo slot. |
| `StopLossPoints` | `int` | `1000` | Distância de stop de proteção opcional em passos de preço. |
| `TakeProfitPoints` | `int` | `2000` | Distância de take-profit opcional em passos de preço. |
| `AllowLongEntry` | `bool` | `true` | Habilitar abertura de posições compradas. |
| `AllowShortEntry` | `bool` | `true` | Habilitar abertura de posições vendidas. |
| `AllowLongExit` | `bool` | `true` | Fechar posições compradas quando aparecem cores baixistas. |
| `AllowShortExit` | `bool` | `true` | Fechar posições vendidas quando aparecem cores altistas. |
| `CandleType` | `DataType` | `H8` | Período de candle para cálculos. |
| `PriceMaMethod` | `SmoothMethod` | `Sma` | Tipo de média móvel usada no preço aplicado. |
| `PriceMaLength` | `int` | `100` | Comprimento do suavizador de preço. |
| `PriceMaPhase` | `int` | `15` | Parâmetro de fase para filtros baseados em Jurik. |
| `RangeMaMethod` | `SmoothMethod` | `Jjma` | Tipo de média móvel usada no intervalo da candle. |
| `RangeMaLength` | `int` | `20` | Comprimento do suavizador de intervalo. |
| `RangeMaPhase` | `int` | `100` | Parâmetro de fase para o suavizador de intervalo. |
| `SmoothingLength` | `int` | `20` | Comprimento de suavização Jurik aplicado ao oscilador e volume. |
| `AppliedPrice` | `AppliedPrice` | `Close` | Fonte de preço usada nos cálculos do oscilador. |
| `VolumeType` | `AppliedVolume` | `Tick` | Fonte de volume multiplicada pelo oscilador. |
| `HighLevel2` | `int` | `150` | Multiplicador extremo superior para o limiar adaptativo. |
| `HighLevel1` | `int` | `20` | Multiplicador moderado superior. |
| `LowLevel1` | `int` | `-20` | Multiplicador moderado inferior. |
| `LowLevel2` | `int` | `-150` | Multiplicador extremo inferior. |
| `SignalBar` | `int` | `1` | Offset histórico usado para ler transições de cor. |

## Notas de Uso
- Conecte a estratégia a um instrumento que forneça informações de preço e volume; quando apenas o volume de ticks está
  disponível, o contador de ticks é usado como proxy.
- Os dois volumes de slot podem ser ajustados independentemente para emular a configuração de gestão de dinheiro dual do EA
  original.
- Ajuste `SignalBar` ao trabalhar com candles parcialmente formadas ou ao ressincronizar dados históricos.
- Os métodos de suavização suportam filtros Jurik através de reflexão para replicar o comportamento da biblioteca MQL
  `SmoothAlgorithms`.
- Como `StartProtection` é invocado apenas quando as distâncias de stop ou alvo são positivas, deixe-as em zero para
  desabilitar as ordens de proteção.
