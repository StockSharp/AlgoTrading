# Estratégia de Histograma RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o histograma do Índice de Força Relativa (RSI) para detectar reversões quando o oscilador abandona zonas extremas. O histograma colore o valor do RSI com base em dois limiares: um nível alto que marca a zona de sobrecompra e um nível baixo que marca a zona de sobrevenda. Quando a cor muda de verde (sobrecompra) para cinza ou vermelho, a estratégia fecha posições vendidas e abre uma posição comprada. Quando a cor muda de vermelho (sobrevenda) para cinza ou verde, fecha posições compradas e abre uma posição vendida.

A implementação é construída com a API de alto nível do StockSharp e subscreve dados de velas de um período selecionado. Um indicador RSI processa as velas e gera sinais sempre que o seu valor sai das zonas definidas. Parâmetros opcionais permitem ativar ou desativar entradas e saídas para cada lado separadamente.

A estratégia destina-se a fins educativos e demonstra como converter um consultor especialista MQL para o framework StockSharp.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A barra anterior estava acima do nível alto e a última barra desceu abaixo dele.
  - **Vendido**: A barra anterior estava abaixo do nível baixo e a última barra subiu acima dele.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O sinal oposto fecha a posição atual se permitido.
- **Stops**: Sem stops integrados; o framework `StartProtection` está preparado para adicioná-los.
- **Valores padrão**:
  - `RSI period` = 14
  - `High level` = 60
  - `Low level` = 40
  - `Timeframe` = 4 hours
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Opcional
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
