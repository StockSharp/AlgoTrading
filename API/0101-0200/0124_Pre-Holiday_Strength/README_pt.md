# Estratégia de Força Pré-Feriado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Força Pré-Feriado refere-se à tendência altista logo antes dos principais feriados do mercado, quando o volume é menor e o sentimento é otimista.
Os traders costumam se posicionar antes do recesso, empurrando os preços para cima na última sessão ou nas duas últimas.

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado cripto.

A estratégia compra no dia anterior ao feriado e sai na sessão seguinte ou no fechamento, capturando esse viés de curto prazo.

Um stop ajustado é usado caso a alta esperada não ocorra.

## Detalhes

- **Critérios de entrada**: ativadores de efeito de calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

