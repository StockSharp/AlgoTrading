# Estratégia do Indicador Stalin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do indicador "Stalin" do MQL5.
Usa um par de médias móveis exponenciais (EMAs) e um filtro RSI opcional.
Um sinal de compra aparece quando a EMA rápida cruza acima da EMA lenta e o RSI está acima de 50.
Um sinal de venda aparece quando a EMA rápida cruza abaixo da EMA lenta e o RSI está abaixo de 50.

Os sinais podem ser confirmados por um movimento de preço necessário e filtrados pela distância do último sinal.
As posições são abertas com ordens a mercado e invertidas em sinais opostos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `FastEMA(t-1) < SlowEMA(t-1)` && `FastEMA(t) > SlowEMA(t)` && `RSI(t) > 50`.
  - **Vendido**: `FastEMA(t-1) > SlowEMA(t-1)` && `FastEMA(t) < SlowEMA(t)` && `RSI(t) < 50`.
- **Confirmar**: A operação é adiada até o preço se mover `Confirm` pontos a partir do nível de rompimento.
- **Filtro Flat**: Novos sinais são ignorados se estiverem mais próximos que `Flat` pontos do preço do sinal anterior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastLength` = 14.
  - `SlowLength` = 21.
  - `RsiLength` = 17.
  - `Confirm` = 0 pontos (desabilitado).
  - `Flat` = 0 pontos (desabilitado).
  - `CandleType` = velas de 1 hora.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Múltiplos.
  - Stops: Não.
  - Complexidade: Moderado.
  - Período: Médio prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
