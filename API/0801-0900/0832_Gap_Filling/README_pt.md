# Estratégia de Preenchimento de Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Preenchimento de Gap busca gaps de preço noturnos no início de uma nova sessão. Quando um gap aparece, a estratégia negocia contra ele esperando um retorno ao preço do dia anterior ou, se invertida, opera na direção do gap com um stop no nível do gap.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Nova sessão e gap de queda (ou gap de alta se invertido).
  - **Vendido**: Nova sessão e gap de alta (ou gap de queda se invertido).
- **Critérios de saída**:
  - Preço de preenchimento do gap atingido (alvo de lucro) ou, quando invertido, o preço atinge o stop no nível do gap.
- **Stops**: Usa o preço da sessão anterior como alvo/stop.
- **Valores padrão**:
  - `CandleType` = 1 minute
  - `Invert` = false
  - `CloseWhen` = NewSession
- **Filtros**:
  - Categoria: Trading de gap
  - Direção: Comprado e Vendido
  - Indicadores: Nenhum
  - Complexidade: Simples
  - Nível de risco: Médio
