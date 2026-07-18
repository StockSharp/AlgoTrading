# Estratégia Color Bears Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa uma estratégia baseada no indicador Color Bears Gap. O indicador compara dois gaps suavizados entre o preço máximo e os valores suavizados de abertura/fechamento. Quando a diferença cruza zero, as posições são abertas na nova direção e as posições opostas são fechadas.

## Detalhes
- **Critérios de entrada**: O indicador cruza abaixo de zero -> comprar; cruza acima de zero -> vender.
- **Comprado/Vendido**: Configurável via parâmetros.
- **Critérios de saída**: Cruzamento oposto da linha zero.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = período de 8 horas
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Color Bears Gap
  - Stops: Não
  - Complexidade: Moderado
  - Período: 8 horas
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
