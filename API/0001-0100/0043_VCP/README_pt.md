# Estratégia Volatility Contraction Pattern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia VCP procura uma sequência de intervalos de preço cada vez mais estreitos. À medida que cada intervalo se contrai, acumula-se energia para um rompimento. O sistema mede o tamanho do intervalo e aguarda uma ruptura acima da máxima mais alta ou abaixo da mínima mais baixa.

Os testes indicam um retorno anual médio de aproximadamente 166%. Funciona melhor no mercado de ações.

Uma vez observada a contração, um rompimento além das extremidades recentes aciona uma operação nessa direção. O cruzamento do preço com a média móvel é usado para gerenciar as saídas.

Esta abordagem visa capturar movimentos explosivos após uma compressão de volatilidade.

## Detalhes

- **Critérios de entrada**: Contração do intervalo e depois rompimento da máxima/mínima recente.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço cruza a MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Range, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

