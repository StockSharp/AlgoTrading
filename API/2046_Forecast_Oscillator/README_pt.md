# Estratégia de Oscilador de Previsão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia adapta o indicador clássico Forecast Oscillator para o StockSharp. Combina uma linha de base de regressão linear com suavização Tillson T3 para destacar reversões de tendência. Um sinal de compra aparece quando o oscilador cruza acima da sua linha suavizada enquanto a linha suavizada permanece abaixo de zero. Um sinal de venda é produzido nas condições opostas.

O algoritmo segue a implementação MQL original e suporta habilitar ou desabilitar a abertura e o fechamento de posições separadamente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O oscilador cruza acima do T3 e o T3 é negativo.
  - **Vendido**: O oscilador cruza abaixo do T3 e o T3 é positivo.
- **Comprado/Vendido**: Ambas as direções são suportadas.
- **Critérios de saída**:
  - Sinais opostos se as opções de fechamento correspondentes estiverem habilitadas.
- **Stops**: Nenhum.
- **Filtros**: Nenhum.
