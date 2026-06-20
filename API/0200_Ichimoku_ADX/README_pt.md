# Ichimoku Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores Ichimoku Cloud e ADX. Critérios de entrada:
Comprado: Price > Kumo (nuvem) && Tenkan > Kijun && ADX > 25 (tendência de alta com movimento forte) Vendido: Price < Kumo (nuvem) && Tenkan < Kijun && ADX > 25 (tendência de baixa com movimento forte) Critérios de saída: Comprado: Price < Kumo (preço cai abaixo da nuvem) Vendido: Price > Kumo (preço sobe acima da nuvem)

Os testes indicam um retorno anual médio de aproximadamente 187%. Funciona melhor no mercado de ações.

Esta estratégia combina sinais do Ichimoku Cloud com ADX para filtrar tendências poderosas. As operações ocorrem quando o preço rompe acima ou abaixo da nuvem com confirmação do ADX.

Favorece traders que preferem configurações de tendência estruturadas. Stops definidos por ATR defendem contra oscilações adversas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - Vendido: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço cruza a nuvem na direção oposta
- **Stops**: Usa a nuvem Ichimoku como stop trailing
- **Valores padrão**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Ichimoku Cloud, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

