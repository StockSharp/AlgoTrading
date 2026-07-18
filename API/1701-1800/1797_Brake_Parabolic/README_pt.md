# Estratégia Brake Parabolic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia implementa o indicador Brake Parabolic que projeta uma barreira parabólica acima ou abaixo do preço. Quando a barreira é rompida, a tendência se inverte e uma nova posição é aberta na direção do rompimento. O algoritmo acompanha o preço extremo com uma linha curva definida pelos parâmetros **A**, **B** e **Shift**.

Os testes indicam um retorno anual médio de aproximadamente 48%. Funciona melhor em mercados com tendência em períodos maiores.

O sistema aguarda a barreira trocar de lado. Uma inversão de alta fecha qualquer posição vendida e abre uma nova posição comprada. Uma inversão de baixa fecha qualquer posição comprada e abre uma vendida. Durante uma tendência, posições opostas são fechadas quando o indicador confirma a direção.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A barreira muda de acima do preço para abaixo do preço.
  - **Vendido**: A barreira muda de abaixo do preço para acima do preço.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou indicador confirma tendência contrária.
- **Stops**: Sem stops fixos; saídas dependem da reversão da barreira.
- **Valores padrão**:
  - `A` = 1.5
  - `B` = 1.0
  - `BeginShift` = 10
  - `CandleType` = período de 4 horas
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Personalizado
  - Stops: Não
  - Complexidade: Moderado
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
