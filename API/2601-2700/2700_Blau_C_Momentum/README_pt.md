# Estratégia Blau C-Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port do StockSharp do consultor especialista de MetaTrader **Exp_BlauCMomentum**. Opera em um único instrumento usando candles de um período configurável e interpreta o Momentum triplicemente suavizado de Blau em um de dois modos:

* **Modo Breakdown** – reage ao cruzamento da linha de momentum pelo nível zero.
* **Modo Twist** – reage a mudanças na direção da inclinação do momentum suavizado.

O indicador é calculado em um período externo e pode opcionalmente usar preços aplicados diferentes para o cálculo do momentum. As posições são abertas com ordens de mercado e podem ser protegidas usando módulos integrados de stop-loss e take-profit.

## Como funciona
1. Inscrever-se em candles do período selecionado.
2. Calcular Blau C-Momentum:
   * O momentum bruto é a diferença entre dois preços aplicados separados por `MomentumLength` barras.
   * O momentum bruto é suavizado três vezes pelo método de média móvel escolhido e escalado para passos de preço (×100/Point).
3. Armazenar o histórico do indicador suavizado para os deslocamentos de barra definidos por `SignalBar`.
4. Gerar sinais:
   * **Breakdown** – se a barra anterior estava acima de zero e a barra de sinal está abaixo ou igual a zero, abrir/inverter comprado; se a barra anterior estava abaixo de zero e a barra de sinal está acima ou igual a zero, abrir/inverter vendido. Os sinalizadores de saída opcionais fecham o lado oposto quando a barra anterior cruza a linha zero.
   * **Twist** – comparar duas barras anteriores; quando o momentum acelera para cima (anterior &lt; mais antigo) e a barra de sinal confirma, abrir/inverter comprado; quando o momentum acelera para baixo (anterior &gt; mais antigo) e a barra de sinal confirma, abrir/inverter vendido. Os sinalizadores de saída opcionais fecham o lado oposto na mesma condição.
5. Usar `MoneyManagement` e `MarginModes` para dimensionar a posição. Valores negativos significam volume fixo; valores positivos arriscam ou alocam uma fração do valor da carteira. Um bloqueio de tempo simples impede reentradas imediatas dentro do mesmo candle.

## Parâmetros
| Grupo | Nome | Descrição |
|-------|------|-------------|
| Negociação | `MoneyManagement` | Participação do capital para dimensionamento de posição. Valor negativo = volume fixo. |
| Negociação | `MarginModes` | Interpretação do gerenciamento de dinheiro (`FreeMarginShare`, `BalanceShare`, `FreeMarginRisk`, `BalanceRisk`). Os modos de risco usam distância de stop-loss e `StepPrice`. |
| Risco | `StopLossPoints` | Distância de stop-loss em passos de preço do instrumento (definir `0` para desabilitar). |
| Risco | `TakeProfitPoints` | Distância de take-profit em passos de preço do instrumento (definir `0` para desabilitar). |
| Negociação | `SlippagePoints` | Slippage permitido (mantido por compatibilidade, não usado para colocação de ordens). |
| Negociação | `EnableLongEntry`, `EnableShortEntry` | Permitir abertura de posições compradas/vendidas. |
| Negociação | `EnableLongExit`, `EnableShortExit` | Permitir fechamento de posições existentes de acordo com o indicador. |
| Lógica | `EntryModes` | `Breakdown` ou `Twist`. |
| Dados | `CandleType` | Período usado para cálculos do indicador (padrão 4h). |
| Indicador | `SmoothingMethod` | Método de média móvel: `Simple`, `Exponential`, `Smoothed`, `LinearWeighted`, `Jurik`, `TripleExponential`, `Adaptive`. |
| Indicador | `MomentumLength` | Profundidade de médias do momentum bruto (barras entre os dois valores de preço). |
| Indicador | `FirstSmoothLength`, `SecondSmoothLength`, `ThirdSmoothLength` | Comprimentos dos três estágios de suavização. |
| Indicador | `Phase` | Parâmetro de fase do Jurik (usado quando o método de suavização é `Jurik`). |
| Indicador | `PriceForClose`, `PriceForOpen` | Preços aplicados usados para o momentum (veja comentários do código para fórmulas). |
| Lógica | `SignalBar` | Índice de barra usado para sinais (0 = barra fechada atual, 1 = barra anterior, etc.). |

## Notas de uso
* Anexe a estratégia a um instrumento e configure a série de candles. O período de negociação é o mesmo que o período do indicador.
* O módulo de proteção da API de alto nível é habilitado automaticamente quando os valores de stop/take profit são positivos.
* Os modos de margem são aproximações porque o StockSharp não expõe balanço/margem livre ao estilo MetaTrader. Os modos baseados em risco dependem de `StopLossPoints` e `Security.StepPrice`.
* Os métodos de suavização avançados da biblioteca original (Parabolic, VIDYA, JurX) são mapeados para os indicadores StockSharp disponíveis mais próximos (`TripleExponential` ≈ T3, `Adaptive` ≈ KAMA).
* O parâmetro de slippage é preservado para completude, mas ordens de mercado são usadas, então o valor é informativo.

## Primeiros passos
1. Configure a conexão, a carteira e o instrumento em seu ambiente StockSharp.
2. Crie uma instância de `BlauCMomentumStrategy`, atribua `Security`, `Portfolio` e os parâmetros desejados.
3. Chame `Start()`; a estratégia assinará candles, calculará o indicador e negociará automaticamente.
4. Monitore os logs para informações sobre posições abertas/fechadas e estados do indicador.

## Aviso de risco
Esta estratégia é fornecida para fins educacionais. Sempre valide o desempenho com testes históricos e prospectivos antes de executá-la em uma conta ao vivo. Ajuste as configurações de risco para corresponder ao seu capital e às condições do mercado.
