# Estratégia Bollinger Winner Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bollinger Winner Lite é um sistema de reversão simplificado que reage quando o
preço se estende além das Bollinger Bands. Ele observa velas grandes que fecham
fora de uma banda e antecipa um rápido retorno para dentro.

O parâmetro `CandlePercent` define quão grande deve ser a vela de rompimento em
relação aos movimentos recentes. Apenas velas que ultrapassam esse limiar acionam
operações, filtrando pequenas flutuações. Por padrão, a estratégia opera apenas
comprado, mas habilitar `ShowShort` permite setups de venda espelhados.

As saídas ocorrem quando o preço toca a banda oposta ou retorna à linha central.
Nenhum stop fixo é usado; o sistema baseia-se na reversão à média.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo da banda inferior com tamanho de vela > `CandlePercent`.
  - **Vendido**: Fechamento acima da banda superior com tamanho de vela > `CandlePercent` (requer `ShowShort`).
- **Critérios de saída**: Toque da banda central ou da banda oposta.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `ShowShort` = false
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado por padrão
  - Indicadores: Bollinger Bands
  - Complexidade: Simples
  - Nível de risco: Médio
