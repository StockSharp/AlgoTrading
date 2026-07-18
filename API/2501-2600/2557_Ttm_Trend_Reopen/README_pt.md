# Estratégia TTM Trend de Reabertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria a lógica do consultor especialista do MetaTrader *Exp_ttm-trend_ReOpen*. Traduz o indicador TTM Trend para o framework StockSharp, usa o suavizado Heikin-Ashi para colorir velas e opera quando a cor muda de baixista para altista ou vice-versa. Cada mudança de cor representa uma mudança de regime na compressão/expansão de volatilidade, portanto o bot fecha imediatamente qualquer exposição oposta e abre uma posição na nova direção.

## Lógica do indicador
O indicador original colore cada barra de acordo com o corpo Heikin-Ashi e a vela OHLC clássica:

- **Verde brilhante (4)** – O fechamento Heikin-Ashi está acima de sua abertura e a vela padrão fecha mais alto do que abre.
- **Azul-petróleo (3)** – Heikin-Ashi está altista mas a vela bruta fecha mais baixo.
- **Rosa intenso (0)** – Heikin-Ashi está baixista e a vela bruta fecha mais baixo.
- **Roxo (1)** – Heikin-Ashi está baixista enquanto a vela bruta fecha mais alto.
- **Cinza (2)** – Fallback neutro se a tendência não puder ser determinada.

Para imitar o suavizado do buffer do MetaTrader, o indicador mantém uma janela deslizante (`CompBars`) de valores Heikin-Ashi anteriores. Se o último corpo permanece dentro do envelope alto/baixo de qualquer vela armazenada, a cor anterior é reutilizada. Isso previne whipsaws durante pequenos recuos, assim como a implementação fonte.

## Regras de trading
1. Assinar o período configurado por `CandleType` e avaliar apenas velas finalizadas (`SignalBar` seleciona quantas barras fechadas observar a partir do último ponto histórico).
2. Quando uma **cor altista** (valores 1 ou 4) aparece e o sinal anterior não era altista:
   - Fechar qualquer vendido se `EnableShortExits` estiver ativo.
   - Abrir uma posição comprada (ou girar de vendido para comprado) se `EnableLongEntries` for verdadeiro.
3. Quando uma **cor baixista** (valores 0 ou 3) aparece e o sinal anterior não era baixista:
   - Fechar qualquer comprado se `EnableLongExits` estiver ativo.
   - Abrir uma posição vendida (ou girar de comprado para vendido) se `EnableShortEntries` for verdadeiro.
4. Cada lado pode piramidear volume adicional quando o preço se move a favor da operação por pelo menos `PriceStepPoints` (convertidos para preço usando o `PriceStep` do instrumento). O número acumulado de entradas por direção é limitado por `MaxPositions`.

## Comportamento de pirâmide
- `PriceStepPoints` reflete o input "PriceStep" do MetaTrader: assim que o lucro não realizado excede esta distância do preço médio de entrada, o bot adiciona o `Volume` base novamente.
- `MaxPositions` limita a contagem total de entradas empilhadas, incluindo a operação inicial. Definir como `1` para desabilitar as reentradas completamente.

## Gestão de risco
`StopLossPoints` e `TakeProfitPoints` são medidos em pontos do instrumento, igual ao EA original. Eles são transformados em distâncias de preço absolutas via `Security.PriceStep` e aplicados através de `StartProtection`. Definir qualquer parâmetro como zero para desabilitar a proteção respectiva.

## Parâmetros
- `CandleType` – período usado para o cálculo de TTM Trend (padrão: velas de 4 horas).
- `CompBars` – número de velas Heikin-Ashi históricas mantidas para suavização de cor (padrão: 6).
- `SignalBar` – número de barras atrás da última vela finalizada a avaliar (padrão: 1 → última barra fechada).
- `PriceStepPoints` – movimento favorável mínimo, em pontos, necessário antes de piramidear (padrão: 300).
- `MaxPositions` – número máximo de entradas acumuladas por direção (padrão: 10).
- `EnableLongEntries` / `EnableShortEntries` – ativar/desativar aberturas compradas/vendidas em mudanças de cor.
- `EnableLongExits` / `EnableShortExits` – ativar/desativar saídas forçadas quando a cor oposta aparece.
- `StopLossPoints` – distância do stop protetor em pontos (padrão: 1000).
- `TakeProfitPoints` – distância do objetivo de lucro em pontos (padrão: 2000).

## Notas de uso
- A lógica de cor TTM Trend é sensível ao período escolhido; o sistema original usava o gráfico H4, mas qualquer `CandleType` pode ser fornecido.
- Como o indicador trabalha com corpos Heikin-Ashi, gaps repentinos podem não acionar uma mudança de cor imediatamente — aguardar a próxima vela finalizada para confirmar.
- Definir `PriceStepPoints` como zero se desejar um sistema de entrada única que nunca piramideia.
