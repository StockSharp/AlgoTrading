# Estratégia de Cruzamento EMA/SMA + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia acompanha três médias móveis exponenciais (rápida, média e lenta)
juntamente com um filtro RSI para participar de tendências emergentes. Uma operação é
acionada quando a média rápida cruza a média na direção da média lenta predominante,
indicando que o impulso está acelerando. Apenas velas que fecham na direção do
cruzamento são consideradas para evitar sinais falsos.

Uma saída protetora pode opcionalmente fechar posições após um número definido pelo
usuário de barras se permanecerem lucrativas. O RSI atua como guarda de
sobrecompra/sobrevenda para sair quando o impulso fica muito esticado.

Backtests mostram que a técnica funciona melhor em pares cripto líquidos durante
fases de tendência onde as médias móveis oferecem separação clara.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `EMA_fast > EMA_medium` e `EMA_fast(t-1) <= EMA_medium(t-1)` e `Close > EMA_slow` e `Close > Open`
  - **Vendido**: `EMA_fast < EMA_medium` e `EMA_fast(t-1) >= EMA_medium(t-1)` e `Close < EMA_slow` e `Close < Open`
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: `RSI > 70` ou `X barras com lucro e Close > entry`
  - **Vendido**: `RSI < 30` ou `X barras com lucro e Close < entry`
- **Stops**: Nenhum.
- **Valores padrão**:
  - `EMA_fast` = 10
  - `EMA_medium` = 20
  - `EMA_slow` = 100
  - `RSI_length` = 14
  - `X bars` = 24
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI
  - Stops: Opcional baseado em tempo
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
