# Estratégia Color X Derivative
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port StockSharp do especialista MetaTrader "Exp_ColorXDerivative". Funciona em um período de velas configurável (velas de 12 horas por padrão) e analisa o histograma de momentum ColorXDerivative. O indicador mede a velocidade de mudança da fonte de preço escolhida durante um deslocamento fixo, suaviza o resultado com uma média móvel e, em seguida, classifica cada barra em um de cinco estados de cor. As operações seguem a mesma lógica que no EA original: o robô compra quando o momentum de alta se acelera ou um movimento de baixa começa a contrair, e vende quando a pressão de baixa aumenta ou uma perna de alta perde força.

## Lógica do Indicador
1. Converter cada vela para o `AppliedPrice` selecionado (fechamento, abertura, fechamento ponderado, Demark, etc.).
2. Calcular a derivada de preço: `(price[0] - price[shift]) * 100 / shift`, onde `shift = DerivativePeriod`.
3. Suavizar a derivada com o método selecionado (`SMA`, `EMA`, `SMMA`, `LWMA` ou `Jurik`). A média móvel Jurik padrão reproduz o suavização JJMA da implementação MQL.
4. Atribuir um estado de cor:
   - **0** – derivada &gt; 0 e crescendo (forte aceleração de alta).
   - **1** – derivada &gt; 0 mas caindo (momentum de alta perdendo força).
   - **2** – derivada ≈ 0 (neutro).
   - **3** – derivada &lt; 0 mas crescendo (movimento de baixa se contraindo).
   - **4** – derivada &lt; 0 e caindo (aceleração de baixa).

Um deslocamento de sinal controla qual barra finalizada é avaliada (1 = última barra fechada, 2 = barra anterior, etc.).

## Regras de Trading
- **Entrada comprada**: habilitada quando `EnableLongEntry` é verdadeiro e:
  - a cor atual é 0 enquanto a cor anterior não era 0 (momentum vira fortemente de alta), ou
  - a cor atual é 3 enquanto a cor anterior era 4 ou 2 (movimento de baixa começa a contrair).
- **Entrada vendida**: habilitada quando `EnableShortEntry` é verdadeiro e:
  - a cor atual é 4 enquanto a cor anterior não era 4 (aceleração de baixa começa), ou
  - a cor atual é 1 enquanto a cor anterior era 0 ou 2 (movimento de alta desvanece).
- **Saída comprada**: acionada quando a cor atual é 1 ou 4 e `EnableLongExit` é verdadeiro.
- **Saída vendida**: acionada quando a cor atual é 0 ou 3 e `EnableShortExit` é verdadeiro.

Ordens são enviadas como ordens a mercado usando o parâmetro `OrderVolume`. Fechamentos de posição são executados antes de novas entradas para emular a lógica sequencial do EA original.

## Gestão de Risco
Distâncias opcionais de stop loss e take profit são fornecidas via `StopLossTicks` e `TakeProfitTicks`. Quando qualquer valor está acima de zero, a estratégia chama `StartProtection`, convertendo ticks em passos de preço usando o tamanho `Step` do instrumento. A proteção de stop/alvo é executada uma vez e é compatível com auto-trading ou backtesting.

## Parâmetros
- `OrderVolume` – tamanho da ordem a mercado.
- `CandleType` – período para os cálculos do indicador (padrão: período de 12 horas).
- `DerivativePeriod` – distância em barras usada para o deslocamento da derivada.
- `AppliedPrice` – fonte de preço passada para a derivada (fechamento, mediana, ponderado, Demark, etc.).
- `SmoothingMethod` – filtro de suavização aplicado à derivada. Valores suportados: SMA, EMA, SMMA, LWMA, Jurik.
- `SmoothingLength` – período do filtro de suavização.
- `SignalShift` – quantas barras finalizadas atrás ler os valores de cor (1 = barra fechada mais recente).
- `StopLossTicks` / `TakeProfitTicks` – distâncias de proteção opcionais em passos do instrumento.
- `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – alternâncias correspondentes às entradas originais do EA.

## Notas
- A estratégia reproduz a lógica orientada por indicador do EA MetaTrader sem recursos adicionais de gestão de dinheiro.
- A suavização Jurik é a aproximação mais próxima do filtro JJMA usado na biblioteca MQL; outras opções mapeiam para as médias móveis padrão do StockSharp.
- O histórico de cores é armazenado internamente para que a otimização em `SignalShift` funcione exatamente como na versão MetaTrader.
