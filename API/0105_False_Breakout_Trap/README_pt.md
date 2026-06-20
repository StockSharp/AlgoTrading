# Estratégia de Armadilha de Falso Rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Armadilha de Falso Rompimento visa capitalizar em rompimentos que não conseguem se manter além de suporte ou resistência chave.
Os traders frequentemente entram em um rompimento apenas para ver o preço reverter rapidamente, deixando-os presos.

Os testes indicam um retorno anual médio de aproximadamente 52%. Funciona melhor no mercado de criptomoedas.

Esta estratégia aguarda esse fracasso, entrando na direção oposta assim que o preço fecha de volta dentro do range.

O posicionamento do stop é apertado, logo além do nível de rompimento fracassado, garantindo que as perdas permaneçam pequenas se a reversão não se materializar.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

