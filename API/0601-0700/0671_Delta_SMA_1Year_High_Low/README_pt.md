# Estratégia Delta SMA Máximo-Mínimo de 1 Ano
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Delta SMA Máximo-Mínimo de 1 Ano** calcula o delta de volume (volume de compra menos volume de venda) e sua média móvel simples. Entra comprado quando o delta SMA estava muito baixo e então cruza acima de zero. A posição é fechada quando o delta SMA cai abaixo de 60% de sua máxima de 1 ano após ter cruzado anteriormente acima de 70% dessa máxima.

## Detalhes
- **Critérios de entrada**: O delta SMA estava abaixo de 70% de sua mínima de 1 ano e cruza acima de zero.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: O delta SMA cai abaixo de 60% de sua máxima de 1 ano após cruzar 70%.
- **Stops**: Não.
- **Valores padrão**:
  - `DeltaSmaLength = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Volume
  - Direção: Comprado
  - Indicadores: SMA, Highest, Lowest
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
