# Estratégia Market Slayer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza um cruzamento de médias móveis ponderadas com confirmação de tendência SSL em um período de tempo superior. Uma posição comprada é aberta quando a WMA curta cruza acima da WMA longa com tendência de alta; uma posição vendida é aberta em condições opostas. Take profit e stop loss absolutos opcionais podem ser habilitados.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: WMA curta cruza acima da WMA longa e o SSL do período superior é de alta.
  - **Vendido**: WMA curta cruza abaixo da WMA longa e o SSL do período superior é de baixa.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O filtro de tendência muda para o lado oposto.
  - Stop loss ou take profit opcionais quando habilitados.
- **Stops**: Opcional.
- **Valores padrão**:
  - `ShortLength` = 10.
  - `LongLength` = 20.
  - `ConfirmationTrendValue` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame().
  - `TakeProfitEnabled` = false.
  - `TakeProfitValue` = 20.
  - `StopLossEnabled` = false.
  - `StopLossValue` = 50.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: WMA, SSL
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
