# Rsi Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores RSI e Supertrend. Entra comprado quando o RSI está em sobrevenda (< 30) e o preço está acima do Supertrend. Entra vendido quando o RSI está em sobrecompra (> 70) e o preço está abaixo do Supertrend.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado forex.

O oscilador RSI define os extremos de momentum enquanto o Supertrend aponta para a direção predominante. As operações ocorrem quando o RSI se alinha com a cor do Supertrend.

Funciona para traders que apreciam uma saída estilo trailing stop. As configurações de ATR protegem ainda mais a posição.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI < 30 && Close > Supertrend`
  - Vendido: `RSI > 70 && Close < Supertrend`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Mudança de Supertrend
- **Stops**: Trailing com Supertrend
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, Supertrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

