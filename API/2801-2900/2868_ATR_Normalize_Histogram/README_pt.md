# Estratégia de Histograma ATR Normalizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Histograma ATR Normalizado reproduz o comportamento do especialista MetaTrader *Exp_ATR_Normalize_Histogram* dentro do StockSharp. O sistema observa a proporção normalizada entre a distância suavizada de fechamento ao mínimo e o range verdadeiro suavizado. Mudanças de cor do histograma impulsionam tanto as entradas quanto as saídas, emulando a lógica multi-buffer utilizada na implementação MQL5 original.

## Cálculo do indicador
1. Para cada vela concluída, a estratégia calcula:
   - `diff = Close − Low`.
   - `range = max(High, fechamento anterior) − min(Low, fechamento anterior)`.
2. Cada série é suavizada independentemente com os métodos e comprimentos selecionados. Cinco métodos estão disponíveis: Simple, Exponential, Smoothed (RMA), Weighted e Jurik. Métodos MQL não suportados (JurX, Parabolic, T3, VIDYA, AMA) recorrem à média móvel simples.
3. O valor normalizado do histograma é calculado como

   `normalized = 100 × smoothedDiff / max(|smoothedRange|, PriceStep)`.
4. Os limiares dividem o histograma em cinco bandas. A transição entre bandas espelha o buffer de cor produzido pelo indicador MQL.

## Lógica de sinais
- **Filtro de entrada** – `SignalBar` seleciona qual barra histórica deve ser avaliada (padrão 1, a última barra fechada). A estratégia compara a cor dessa barra com a anterior:
  - Uma transição do extremo altista (cor `0`) para qualquer outra cor abre uma posição comprada quando operações compradas estão habilitadas.
  - Uma transição do extremo baixista (cor `4`) para qualquer outra cor abre uma posição vendida quando operações vendidas estão habilitadas.
- **Filtro de saída** – a cor da barra anterior por si só é suficiente para fechar posições:
  - A cor `0` fecha posições vendidas se as saídas vendidas estiverem habilitadas.
  - A cor `4` fecha posições compradas se as saídas compradas estiverem habilitadas.
- As saídas são processadas antes de qualquer nova entrada para que a estratégia nunca mantenha operações sobrepostas.

## Gestão de risco
A estratégia mantém o registro do último preço de execução e opcionalmente aplica stops de proteção e metas medidos em pontos do instrumento. A conversão usa `Security.PriceStep`, correspondendo ao conceito de "pontos" do especialista original. Quando qualquer limite é atingido dentro da barra, a posição é fechada imediatamente e a direção da operação pode mudar no sinal seguinte.

## Parâmetros
- `CandleType` – período usado para o cálculo.
- `FirstSmoothingMethod` / `SecondSmoothingMethod` – tipo de suavização para os fluxos `diff` e `range`.
- `FirstLength` / `SecondLength` – períodos para os suavizadores.
- `HighLevel`, `MiddleLevel`, `LowLevel` – limiares do histograma (padrão 60/50/40).
- `SignalBar` – deslocamento para avaliação do buffer (mínimo 1).
- `EnableBuyEntries`, `EnableSellEntries`, `EnableBuyExits`, `EnableSellExits` – interruptores para gerenciar as quatro direções de operação.
- `TradeVolume` – tamanho de ordem base. A estratégia compensa automaticamente a exposição existente ao mudar de direção.
- `StopLossPoints`, `TakeProfitPoints` – distâncias de proteção opcionais em pontos; definir como zero para desabilitar.

## Notas e diferenças em relação à versão MQL
- Ambos os estágios de suavização são configuráveis independentemente, mas apenas as cinco implementações de média móvel do StockSharp estão disponíveis. Quando outro método MQL é selecionado, a estratégia usa a média móvel simples mantendo o comprimento.
- A lógica de `SignalBar` segue o deslocamento de buffer usado em `CopyBuffer`, portanto deslocamentos maiores ainda comparam a barra escolhida com seu predecessor imediato.
- Os parâmetros de gestão de dinheiro do especialista original (`MM`, `MMMode`, `Deviation`) são simplificados para um único parâmetro `TradeVolume`. A execução de ordens ocorre a mercado com monitoramento opcional de stop/alvo.
