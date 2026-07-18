# Estratégia Color JJRSX Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reimagina o consultor especialista MetaTrader `Exp_ColorJJRSX` dentro do framework de alto nível do StockSharp. O sistema original depende do oscilador ColorJJRSX proprietário, que combina técnicas de suavização Jurik para detectar mudanças de tendência. Neste port, o oscilador é aproximado com um Índice de Força Relativa (RSI) padrão que é suavizado adicionalmente por uma Média Móvel Jurik (JMA). A inclinação do oscilador suavizado é então avaliada ao longo de várias barras históricas para acionar entradas e saídas.

A negociação ocorre em um período de vela configurável (velas de 4 horas por padrão) e suporta alternadores independentes para operações compradas e vendidas. Parâmetros adicionais permitem manter a lógica de saída idêntica ao consultor especialista fonte enquanto introduz controles de risco nativos do StockSharp, como stop loss e take profit baseados em pontos.

## Construção do indicador
1. **Aproximação RSI** – Um `RelativeStrengthIndex` com o período definido por `JurxPeriod` substitui o estágio original de suavização JurX. Isso mantém o oscilador limitado entre 0 e 100 enquanto captura o momentum relativo.
2. **Suavização Jurik** – A saída do RSI é passada por uma `JurikMovingAverage` (comprimento `JmaPeriod`). A série resultante é uma curva suave que reage rapidamente às mudanças de momentum sem lag excessivo.
3. **Janela histórica** – A estratégia armazena os valores JMA mais recentes `SignalBar + 3` para replicar o uso de `CopyBuffer` do MQL. Os valores indexados por `SignalBar`, `SignalBar + 1` e `SignalBar + 2` correspondem às barras usadas no especialista fonte para avaliação de sinais.

## Lógica de negociação
- **Setup altista**
  - `JMA[SignalBar + 1] < JMA[SignalBar + 2]` confirma que o oscilador virou para cima na barra precedente.
  - `JMA[SignalBar] > JMA[SignalBar + 1]` mostra que o momentum ascendente continua na última barra fechada.
  - Se as entradas compradas estiverem habilitadas e nenhuma posição comprada estiver ativa, a estratégia compra `OrderVolume` unidades. A exposição vendida existente é revertida automaticamente.
- **Setup baixista**
  - `JMA[SignalBar + 1] > JMA[SignalBar + 2]` confirma uma virada para baixo.
  - `JMA[SignalBar] < JMA[SignalBar + 1]` valida o momentum descendente contínuo.
  - Se as entradas vendidas estiverem habilitadas, a estratégia vende `OrderVolume` unidades e inverte qualquer exposição comprada existente.
- **Regras de saída**
  - Quando a inclinação do oscilador suavizado vira contra a posição (`AllowBuyClose` / `AllowSellClose`), a operação aberta é fechada ao mercado.
  - Níveis de stop loss e take profit de proteção (expressos em pontos de preço) são recalculados em cada nova posição. Se o intervalo da vela tocar um nível, a posição é fechada imediatamente.

## Gestão de risco
- `StopLossPoints` converte para distância de preço com o passo de preço do instrumento e protege contra movimentos adversos.
- `TakeProfitPoints` define a distância de alvo simétrica.
- Stops e alvos são desabilitados automaticamente quando definidos como zero.
- O volume pode ser ajustado independentemente do volume da estratégia base através de `OrderVolume`.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `JurxPeriod` | Período da aproximação RSI usada antes da suavização Jurik. Espelha o período JurX do especialista MQL. |
| `JmaPeriod` | Comprimento da Média Móvel Jurik aplicada à saída do RSI. |
| `SignalBar` | Índice da barra histórica usada para avaliação (1 = barra fechada anterior). Valores maiores atrasam a confirmação do sinal. |
| `EnableBuy` / `EnableSell` | Alternar entradas compradas ou vendidas independentemente. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar sinais de saída baseados em inclinação para posições compradas e vendidas respectivamente. |
| `OrderVolume` | Volume negociado em cada nova entrada. A exposição oposta existente é adicionada à nova ordem para realizar uma reversão completa. |
| `TakeProfitPoints` / `StopLossPoints` | Alvo de lucro e distância de stop em pontos do instrumento. Definir como zero para desabilitar. |
| `CandleType` | Período de vela usado para cálculos do indicador (padrão velas de 4 horas). |

## Diferenças do consultor especialista original
- A suavização JurX é aproximada por um RSI clássico porque o algoritmo JurX proprietário não está disponível no StockSharp. Os nomes dos parâmetros permanecem consistentes para simplificar a migração.
- O deslizamento do MetaTrader (`Deviation_`) e as enumerações de gerenciamento de dinheiro não são reproduzidos. Em vez disso, um parâmetro `OrderVolume` fixo é fornecido; você pode combiná-lo com módulos de dimensionamento de posição do StockSharp, se necessário.
- As ordens são executadas com `BuyMarket`/`SellMarket`, enquanto stop loss e take profit são emulados através de verificações de preço na vela finalizada.

## Dicas de uso
1. Vincule a estratégia ao instrumento desejado e defina `CandleType` para corresponder ao período que deseja replicar.
2. Ajuste `JurxPeriod` e `JmaPeriod` para se adaptar à capacidade de resposta do mercado. Valores mais altos criam oscilações mais suaves e menos sinais.
3. Ajuste `SignalBar` se precisar de lag de confirmação adicional em comparação com o atraso padrão de uma barra.
4. Configure `OrderVolume`, `StopLossPoints` e `TakeProfitPoints` de acordo com seu apetite de risco. Use zero para desabilitar saídas automáticas.
5. Combine com os helpers de registro ou gráficos integrados do StockSharp (já conectados para gráficos de velas + indicadores) para monitorar o comportamento do oscilador em tempo real.

A estratégia está pronta tanto para experimentação discricionária quanto para backtesting automatizado dentro do ambiente StockSharp, permanecendo fiel à intenção do sistema ColorJJRSX original.
