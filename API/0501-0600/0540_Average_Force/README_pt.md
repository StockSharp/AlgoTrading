# Estratégia de Força Média
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Força Média utiliza um oscilador que mede onde o fechamento se encontra dentro do máximo mais alto e do mínimo mais baixo de um período de observação e suaviza o resultado com uma média móvel. Valores positivos sinalizam pressão de alta, enquanto valores negativos mostram força de baixa.

A estratégia vai comprado quando o valor suavizado da Força Média está acima de zero e vai vendido quando está abaixo de zero.

## Detalhes

- **Critérios de entrada**:
  - Average Force > 0 → Comprar.
  - Average Force < 0 → Vender.
- **Comprado/Vendido**: Ambas as posições compradas e vendidas.
- **Critérios de saída**:
  - A posição se reverte quando a Força Média cruza zero na direção oposta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Period` = 18
  - `Smooth` = 6
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Highest, Lowest, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
