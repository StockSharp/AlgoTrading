# Estratégia DEMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera os cruzamentos entre o RSI de uma média móvel exponencial dupla e seu valor suavizado. Uma posição comprada é aberta quando o RSI cruza acima da linha suavizada e uma posição vendida no cruzamento oposto. As posições podem ser protegidas com take profit, trailing stop e um filtro de sessão de trading opcional.

## Parâmetros
- Tipo de vela
- Comprimento de MA
- Comprimento de RSI
- Comprimento de suavização de RSI
- Pontos de take profit
- Pontos de trailing stop
- Usar sessão
- Início da sessão
- Fim da sessão

### Regras reais de RSI(DEMA)

Padrões: `CandleType=1m`, `MaLength=20`, `RsiLength=14`, `RsiSmoothingLength=5`, `TakeProfitPoints=0`, `TrailStopPoints=0`, `UseSession=false`, `SessionStart=09:00`, `SessionEnd=17:00`. Em candle concluído: `EMA1=EMA(Close,20)`, `EMA2=EMA(EMA1,20)`, `DEMA=2×EMA1−EMA2`, `R=RSI(DEMA,14)`, `S=SMA(R,5)`. Compra quando antes `R <= S` e agora `R > S`; vende quando antes `R >= S` e agora `R < S`, podendo inverter posição com `Volume + abs(Position)`. Com `UseSession=true`, apenas entradas dentro de `[SessionStart,SessionEnd)`, inclusive sessões noturnas; proteções encerram independentemente do horário. `TakeProfitPoints` e `TrailStopPoints` são **ticks mínimos de preço**: valores positivos ativam TP e trailing nativos em distância absoluta, zero desativa. O README original não detalhava defaults numéricos, definidos nesta implementação. O cruzamento EMA120/450 anterior não satisfazia o contrato.

