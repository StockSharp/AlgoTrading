# Estratégia de Reversão do Canal Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os Canais Donchian marcam as máximas e mínimas recentes durante um período escolhido. Preços que perfuram esses limites e depois se revertem podem sinalizar exaustão. Esta estratégia observa fechamentos de volta dentro do canal após uma ruptura breve.

Os testes indicam um retorno anual médio de aproximadamente 157%. Funciona melhor no mercado de criptomoedas.

Se o fechamento anterior estava abaixo da banda inferior e o fechamento atual sobe de volta acima dela, uma operação comprada é realizada. De forma oposta, se o fechamento anterior estava acima da banda superior e o preço cai de volta para dentro, uma posição vendida é aberta. Um stop percentual gerencia o risco em ambos os casos.

Operando apenas após uma ruptura frustrada, esta abordagem tenta capturar movimentos falsos que recuam rapidamente.

## Detalhes

- **Critérios de entrada**: O preço fecha de volta dentro do Canal Donchian após romper a banda superior ou inferior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `Period` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Donchian Channel
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

