# Estratégia de Reversão por Pavio Baixista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando uma vela baixista forma um longo pavio inferior que excede um limiar percentual definido pelo usuário. Um filtro EMA opcional exige que o fechamento esteja acima de uma média móvel para confirmar a direção da tendência. As posições são fechadas quando o preço fecha acima da máxima da vela anterior.

## Detalhes

- **Critérios de entrada:** vela baixista com pavio inferior <= limiar e dentro da janela de negociação; opcionalmente preço acima da EMA.
- **Comprado/Vendido:** Somente comprado.
- **Critérios de saída:** preço de fechamento > máxima anterior.
- **Stops:** Nenhum.
- **Valores padrão:**
  - Limiar = -1 (%)
  - Filtro EMA desativado, período EMA = 200
  - Hora de início = 2014-01-01, Hora de fim = 2099-01-01
  - Período do candle = 1 minuto
- **Filtros:**
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
