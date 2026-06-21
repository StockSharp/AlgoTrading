# Estratégia Twisted SMA 4h
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Twisted SMA usa três médias móveis simples e um filtro KAMA em velas de 4 horas. Uma posição comprada é aberta quando a SMA rápida está acima da média, a média acima da lenta, o preço acima de uma SMA mais longa e a KAMA não está plana. A posição é encerrada quando as SMAs se alinham de forma baixista.

## Detalhes

- **Critérios de entrada**: SMA rápida > SMA média > SMA lenta, fechamento > SMA principal, KAMA não plana.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: SMA rápida < SMA média < SMA lenta.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: SMA, KAMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
