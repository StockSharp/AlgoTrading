# Estratégia de Reversão à Média IBS de Força Interna da Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão à média somente vendida utilizando a Força Interna da Barra (IBS). Vende quando o IBS está alto e o preço rompe acima da máxima anterior; encerra quando o IBS cai abaixo do limiar inferior.

## Detalhes

- **Critérios de entrada**: IBS >= limiar superior e fechamento > máxima anterior
- **Comprado/Vendido**: Vendido
- **Critérios de saída**: IBS <= limiar inferior
- **Stops**: Não
- **Valores padrão**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Vendido
  - Indicadores: IBS
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
