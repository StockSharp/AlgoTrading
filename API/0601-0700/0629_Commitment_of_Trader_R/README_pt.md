# Estratégia Commitment of Trader R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador Williams %R para detectar condições de sobrecompra e sobrevenda. Uma média móvel simples atua como filtro de tendência opcional.

Uma operação comprada é aberta quando Williams %R sobe acima do limiar superior e o preço de fechamento está acima da SMA. Uma operação vendida é aberta quando Williams %R cai abaixo do limiar inferior e o preço está abaixo da SMA. As posições são fechadas quando o oscilador sai da zona de sinal.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %R > limiar superior e (preço > SMA se habilitado)
  - **Vendido**: %R < limiar inferior e (preço < SMA se habilitado)
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: %R < limiar superior
  - **Vendido**: %R > limiar inferior
- **Stops**: Não
- **Valores padrão**:
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Williams %R, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
