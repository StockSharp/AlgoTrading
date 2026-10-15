# Estratégia de Soma de Volume dos Principais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia soma o volume com sinal das velas recentes e opera quando a soma de curto prazo excede uma fração do seu máximo histórico.

## Detalhes

- **Critérios de entrada**:
  - A soma de volume com sinal de 10 períodos está acima de `Threshold` × máximo e sem posição: entrar comprado.
  - A soma de volume com sinal de 10 períodos está abaixo de `-Threshold` × máximo e sem posição: entrar vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto fecha a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Threshold` = 0.75
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
