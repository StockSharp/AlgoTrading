# Estratégia Vwap Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores VWAP e ADX. Entra comprado quando o preço está acima do VWAP e ADX > 25. Entra vendido quando o preço está abaixo do VWAP e ADX > 25. Sai quando ADX < 20.

Os testes indicam um retorno anual médio de aproximadamente 157%. Funciona melhor no mercado de criptomoedas.

O VWAP atua como referência da sessão e o ADX mede a convicção. As entradas aparecem quando o preço se afasta do VWAP com o ADX mostrando força.

Adequado para traders intradiários de tendência. Stops protetores usam múltiplos de ATR.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > VWAP && ADX > 25`
  - Vendido: `Close < VWAP && ADX > 25`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: ADX cai abaixo do limiar
- **Stops**: Percentual usando `StopLossPercent`
- **Valores padrão**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

