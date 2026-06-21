# Estratégia MOC Delta MOO Entry v2 Reverse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia inverte a lógica clássica do MOC Delta MOO Entry. Mede o delta de volume de compra-venda na sessão da tarde (14:50–14:55) e armazena o delta como porcentagem do volume do dia. Na manhã seguinte às 08:30, uma posição é aberta na direção oposta ao delta se ele ultrapassar um limiar, filtrado por duas médias móveis. As posições são encerradas com take profit e stop loss baseados em ticks ou às 14:50.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Às 08:30, quando o percentual de delta salvo está abaixo de `-DeltaThreshold` e o preço de abertura está acima do SMA15 e SMA30, com SMA15 acima do SMA30.
  - **Vendido**: Às 08:30, quando o percentual de delta salvo está acima de `DeltaThreshold` e o preço de abertura está abaixo do SMA15 e SMA30, com SMA15 abaixo do SMA30.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Take profit e stop loss em ticks.
  - Encerramento de todas as posições abertas às 14:50.
- **Stops**:
  - `TpTicks` = 20 ticks de take profit.
  - `SlTicks` = 10 ticks de stop loss.
- **Valores padrão**:
  - `DeltaThreshold` = 2
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
