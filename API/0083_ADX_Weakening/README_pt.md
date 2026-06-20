# Estratégia de Enfraquecimento do ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Average Directional Index mede a força da tendência. Quando o ADX começa a declinar, frequentemente sinaliza que o movimento atual está perdendo momentum. Este sistema opera contra essa tendência enfraquecida quando o preço está do lado oposto de uma média móvel simples.

Os testes indicam um retorno anual médio de aproximadamente 136%. Funciona melhor no mercado de ações.

Para cada barra, a estratégia calcula o ADX e uma MA. Se o ADX diminui em relação ao valor anterior e o preço está acima da MA, é colocada uma entrada comprada. Se o ADX cair enquanto o preço está abaixo da MA, vai vendido. Um stop-loss fixo protege a posição.

Como a abordagem antecipa uma desaceleração em vez de uma reversão completa, as operações geralmente são mantidas apenas até o ADX começar a subir novamente ou o stop ser atingido.

## Detalhes

- **Critérios de entrada**: ADX menor que o valor anterior e preço relativo à MA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

