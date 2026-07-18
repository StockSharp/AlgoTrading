# Estratégia ADX DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza o Índice de Movimento Direcional (DMI) para negociar cruzamentos entre as linhas +DI e -DI. Quando -DI sobe acima de +DI e depois cai abaixo dele, a estratégia abre uma posição comprada. Quando +DI sobe acima de -DI e depois cai abaixo, abre uma posição vendida. Sinais opostos podem opcionalmente fechar posições existentes.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: -DI estava acima de +DI na barra anterior e cruza abaixo na barra mais recente.
  - **Vendido**: +DI estava acima de -DI na barra anterior e cruza abaixo na barra mais recente.
- **Critérios de saída**:
  - Cruzamento inverso se a opção de fechamento correspondente estiver habilitada.
- **Indicadores**:
  - Directional Index (período 14 por padrão)
- **Stops**: nenhum por padrão.
- **Valores padrão**:
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **Filtros**:
  - Funciona em qualquer período
  - Indicadores: DMI
  - Stops: opcional via gerenciamento de risco externo
  - Complexidade: básico
