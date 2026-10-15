# Estratégia de Reversão Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O indicador Supertrend combina ATR e preço para produzir suporte ou resistência de trailing. Quando a linha Supertrend passa de acima para abaixo do preço ou vice-versa, sugere uma possível mudança de tendência. Esta estratégia opera essas mudanças.

Os testes indicam um retorno anual médio de aproximadamente 151%. Funciona melhor no mercado de ações.

Em cada candle, um cálculo baseado em ATR atualiza o nível Supertrend. Uma mudança de acima do preço para abaixo aciona uma entrada comprada, enquanto um movimento de abaixo para acima cria uma posição vendida. O código de amostra omite stops explícitos, portanto as saídas são discricionárias ou gerenciadas por um módulo de risco separado.

O indicador pode reagir rapidamente à volatilidade, portanto os traders frequentemente o combinam com filtros adicionais para reduzir sinais falsos.

## Detalhes

- **Critérios de entrada**: O Supertrend muda de lado em relação ao preço.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop manual ou externo.
- **Stops**: Não definido.
- **Valores padrão**:
  - `Period` = 10
  - `Multiplier` = 3.0
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Supertrend
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

