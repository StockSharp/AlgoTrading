# Alerta de Varredura de Sessão Principal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora as sessões diárias e detecta quando a sessão atual varre a máxima ou mínima da sessão anterior. Quando uma varredura ocorre e a vela fecha de volta dentro do intervalo anterior, uma operação é aberta na direção oposta com uma relação risco-retorno configurável.

## Detalhes

- **Critérios de entrada**: Varredura da máxima/mínima da sessão anterior com filtro opcional de fechamento de vela.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop no extremo da sessão ou alvo baseado no risco-retorno.
- **Stops**: Sim.
- **Valores padrão**:
  - `MinRiskReward` = 1
  - `UseCandleFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Price Action
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
