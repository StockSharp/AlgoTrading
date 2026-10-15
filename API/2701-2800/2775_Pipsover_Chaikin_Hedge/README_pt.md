# Estratégia Pipsover Chaikin Hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o consultor especialista "Pipsover 2" do MetaTrader dentro do StockSharp. Ela busca condições de
sobrevenda ou sobrecompra com o oscilador Chaikin enquanto o preço perfura uma média móvel e usa o corpo da vela anterior para
confirmar a reversão. O port StockSharp mantém a lógica de hedge discrecionária do código original: quando um sinal oposto
aparece enquanto já há uma posição, a estratégia imediatamente reverte a exposição líquida para seguir o novo viés.

## Indicadores e dados
- **Oscilador Chaikin**: construído a partir de uma linha de Acumulação/Distribuição suavizada por duas médias móveis. Ambas as
  médias são configuráveis e correspondem à implementação do MetaTrader (simples, exponencial, suavizada ou ponderada).
- **Média móvel de preço**: comprimento, deslocamento e tipo configuráveis. Serve como a âncora de reversão à média que os
  máximos ou mínimos da vela anterior devem perfurar.
- **Período**: a estratégia subscreve a um único fluxo de velas escolhido através do parâmetro `CandleType`.

## Lógica de trading
1. Trabalhar apenas com velas concluídas. O corpo da vela anterior (fechamento vs. abertura) fornece o viés direcional.
2. Ler o valor do oscilador Chaikin da vela anterior. Valores negativos grandes sinalizam sobrevenda, valores positivos grandes marcam zonas de sobrecompra.
3. Exigir que a vela anterior perfure o valor atual da média móvel (`Low < MA` para configurações de alta e `High > MA` para as de baixa).
4. Entrar quando não há posição aberta:
   - **Comprado**: vela anterior de alta, mínimo abaixo da MA, Chaikin abaixo de `-OpenLevel`.
   - **Vendido**: vela anterior de baixa, máximo acima da MA, Chaikin acima de `OpenLevel`.
5. Quando uma posição existe e uma configuração oposta aparece, o algoritmo reverte a posição líquida (`SellMarket` / `BuyMarket` com volume extra) para espelhar o comportamento de hedge da versão MT5.
6. Stops e alvos são emulados dentro da estratégia usando máximos/mínimos de velas, porque o StockSharp trabalha com posições líquidas em vez de tickets individuais cobertos.

## Gerenciamento de risco
- **Stop-loss e take-profit**: distâncias em pips convertidas através do passo de preço do instrumento. Ambos podem ser desabilitados com zero.
- **Break-even**: uma vez que o preço se move `BreakevenPips` a favor, o stop é movido para o preço de entrada.
- **Trailing**: após o movimento exceder `BreakevenPips + TrailingStopPips`, o stop segue o preço à distância de trailing.
- **Redefinição do estado de posição**: sempre que uma saída ocorre, todos os níveis de preço internos são limpos para preparação do próximo trade.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| `OpenLevel` | Magnitude do Chaikin necessária para abrir uma nova posição (padrão 100). |
| `CloseLevel` | Magnitude do Chaikin necessária para reverter uma posição existente (padrão 125). |
| `StopLossPips` | Distância do stop-loss em pips (padrão 65). |
| `TakeProfitPips` | Distância do take-profit em pips (padrão 100). |
| `TrailingStopPips` | Distância de trailing em pips (padrão 30). |
| `BreakevenPips` | Ganho em pips antes de mover o stop para break-even (padrão 15). |
| `MaPeriod` | Comprimento da média móvel para o filtro de preço (padrão 20). |
| `MaShift` | Barras para deslocar a média móvel (padrão 0). |
| `MaType` | Tipo de média móvel (Simple, Exponential, Smoothed, Weighted). |
| `ChaikinFastPeriod` | Comprimento de suavização rápida no oscilador Chaikin (padrão 3). |
| `ChaikinSlowPeriod` | Comprimento de suavização lenta no oscilador Chaikin (padrão 10). |
| `ChaikinMaType` | Tipo de média móvel usada para suavização do Chaikin. |
| `CandleType` | Série de velas usada para cálculos. |

## Notas
- Configurar a propriedade base `Volume` no StockSharp para controlar o tamanho da operação.
- Os pips são calculados usando o `PriceStep` do instrumento. Se o passo corresponde a cotações de 3 ou 5 decimais (p.ex., 0.00001), a estratégia o multiplica por 10 para corresponder ao espaçamento de pips do MetaTrader.
- Como o StockSharp usa posições líquidas, as ordens de hedge do consultor especialista MQL original são representadas como reversões imediatas da posição existente.
