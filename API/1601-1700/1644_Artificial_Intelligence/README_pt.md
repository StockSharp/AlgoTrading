# Estratégia de Inteligência Artificial com Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Inteligência Artificial utiliza um perceptron simples para combinar múltiplas leituras do Oscillador Acelerador (AC) em diferentes deslocamentos temporais. A soma ponderada do valor atual do AC e três valores defasados (7, 14, 21 barras atrás) determina a direção da operação. Quando a saída do perceptron é positiva, a estratégia abre ou mantém uma posição comprada; quando é negativa, abre ou mantém uma posição vendida.

Após uma entrada, a estratégia protege a operação com um stop-loss expresso em pontos. À medida que o preço se move na direção lucrativa, o nível de stop segue o preço. Se a saída do perceptron muda de sinal enquanto a posição é lucrativa, a estratégia reverte, fechando a posição atual e entrando na oposta.

Os testes mostram que essa abordagem pode reagir rapidamente às mudanças de momentum mantendo o risco sob controle. Funciona em qualquer instrumento que forneça dados de velas e não depende de regimes de mercado específicos.

## Detalhes

- **Critérios de entrada**  
  - **Comprado**: Saída do perceptron > 0 e sem posição comprada existente.  
  - **Vendido**: Saída do perceptron < 0 e sem posição vendida existente.
- **Saída / Reversão**  
  - Stop trailing ativado.  
  - A saída do perceptron muda de sinal; a estratégia reverte a posição.
- **Stops**: Sim, stop trailing baseado no parâmetro `StopLoss`.
- **Valores padrão**  
  - `X1 = 135`  
  - `X2 = 127`  
  - `X3 = 16`  
  - `X4 = 93`  
  - `StopLoss = 85`
- **Filtros**  
  - Categoria: Momentum  
  - Direção: Ambos  
  - Indicadores: Accelerator Oscillator  
  - Stops: Sim  
  - Complexidade: Médio  
  - Período: Curto prazo  
  - Redes neurais: Perceptron  
  - Divergência: Não  
  - Nível de risco: Médio
