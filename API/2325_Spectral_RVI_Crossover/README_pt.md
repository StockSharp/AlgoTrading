# Estratégia de Cruzamento Spectral RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Spectral RVI Crossover suaviza o Relative Vigor Index e sua linha de sinal e opera nos seus cruzamentos.
Compra quando o RVI suavizado cruza acima da linha de sinal suavizada e vende quando ocorre o oposto.

## Detalhes

- **Critérios de entrada**: cruzamento do RVI suavizado com sua linha de sinal suavizada
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RVI, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: 4 horas
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
