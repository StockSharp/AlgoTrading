# Estratégia Blau Ergodic MDI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Blau Ergodic Market Directional Indicator (MDI) reproduz o comportamento do consultor especialista do MetaTrader `Exp_BlauErgodicMDI`. O algoritmo opera em um fluxo de velas de período superior (padrão 4H) e aplica um pipeline de suavização tripla à entrada de preço selecionada para construir um histograma de momentum e uma linha de sinal. As decisões de trading são derivadas desse histograma usando um de três modos de entrada configuráveis:

1. **Breakdown** – opera quando o histograma cruza a linha zero.
2. **Twist** – reage a reversões na inclinação do histograma (momentum mudando de direção).
3. **CloudTwist** – atua em cruzamentos do histograma/linha de sinal.

Cada sinal pode opcionalmente fechar posições opostas e/ou abrir novas negociações dependendo dos sinalizadores de permissão fornecidos pelo usuário.

## Lógica do indicador
1. Suavizar o preço aplicado escolhido com o tipo de média móvel configurado e `PrimaryLength` para obter o preço base.
2. Calcular a diferença de momentum `(price - baseline) / point_value`.
3. Suavizar esse momentum com `FirstSmoothingLength` e `SecondSmoothingLength` para construir o histograma.
4. Suavizar o histograma mais uma vez com `SignalLength` para obter a linha de sinal.
5. Armazenar em buffer valores históricos de acordo com `SignalBarShift` para que os sinais possam ser confirmados em velas fechadas.

As famílias de suavização suportadas são **EMA**, **SMA**, **SMMA/RMA** e **WMA**. A seleção do preço aplicado espelha a implementação do MetaTrader (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado, simples, quarto, variantes de acompanhamento de tendência).

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `Volume` | Tamanho da ordem usado ao abrir posições. |
| `StopLossPoints` | Distância do stop-loss em pontos do instrumento (0 desativa). |
| `TakeProfitPoints` | Distância do take-profit em pontos do instrumento (0 desativa). |
| `SlippagePoints` | Derrapagem de preço máxima em pontos aplicada a ordens a mercado. |
| `AllowLongEntries` / `AllowShortEntries` | Permitir abrir posições na respectiva direção. |
| `AllowLongExits` / `AllowShortExits` | Permitir fechar posições existentes em sinais opostos. |
| `Mode` | Modo de entrada (Breakdown / Twist / CloudTwist). |
| `CandleType` | Período das velas usadas para cálculos (padrão 4H). |
| `SmoothingMethods` | Família de média móvel usada em todos os passos de suavização. |
| `PrimaryLength` | Comprimento de suavização base para o preço aplicado. |
| `FirstSmoothingLength` | Primeiro comprimento de suavização aplicado ao momentum. |
| `SecondSmoothingLength` | Segundo comprimento de suavização formando o histograma. |
| `SignalLength` | Comprimento de suavização do histograma para criar a linha de sinal. |
| `AppliedPrices` | Fonte de preço usada nos cálculos do indicador. |
| `SignalBarShift` | Número de barras fechadas a serem analisadas ao avaliar sinais. |
| `Phase` | Parâmetro reservado para compatibilidade (não usado na implementação atual). |

## Condições de sinal
* **Breakdown**
  * Comprado: histograma em `SignalBarShift` é positivo enquanto a barra anterior não é.
  * Vendido: histograma em `SignalBarShift` é negativo enquanto a barra anterior não é.
* **Twist**
  * Comprado: histograma em `SignalBarShift` está subindo após um período de queda (anterior < mais recente e duas barras atrás > anterior).
  * Vendido: histograma em `SignalBarShift` está caindo após um período de subida (anterior > mais recente e duas barras atrás < anterior).
* **CloudTwist**
  * Comprado: histograma cruza acima da linha de sinal (histograma mais recente > sinal mais recente, histograma anterior <= sinal anterior).
  * Vendido: histograma cruza abaixo da linha de sinal.

Cada sinal pode tanto nivelar a exposição oposta (se saídas forem permitidas) quanto abrir uma nova negociação com o volume configurado.

## Gestão de risco
`StartProtection` é inicializado com as distâncias de stop-loss e take-profit especificadas (convertidas de pontos para unidades de preço usando o tamanho de tick do instrumento). Se qualquer distância for zero, a proteção respectiva é omitida. A derrapagem também é convertida para unidades de preço usando o mesmo tamanho de tick.

## Notas
* Os sinais são processados apenas em velas finalizadas para espelhar o comportamento original do MetaTrader.
* `SignalBarShift` permite atrasar a confirmação de negociações para evitar agir na barra mais recente.
* O parâmetro `Phase` é mantido por completude, mas não tem efeito ao usar os métodos de suavização suportados.
* Todos os comentários de código são fornecidos em inglês para simplificar a manutenção futura.
