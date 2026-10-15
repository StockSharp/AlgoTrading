# Estratégia de Barra de Reversão Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia - Barra de Reversão Altista. Entra comprado quando uma barra de reversão altista se forma abaixo das linhas do Alligator e o preço rompe acima da máxima da barra. Filtros opcionais podem habilitar o Awesome Oscillator e as barras squat do Market Facilitation Index.

A configuração busca uma nova mínima que feche na metade superior do candle enquanto a tendência se torna altista. A confirmação vem quando o preço supera a máxima da barra.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `bullish reversal bar && close > confirmation level`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - Stop-loss na mínima da barra ou quando a tendência vira para baixo
- **Stops**: Mínima da barra armazenada em `_stopLoss`
- **Valores padrão**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: Alligator, Awesome Oscillator, Market Facilitation Index
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
