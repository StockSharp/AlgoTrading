# OBV Modificado com Detecção de Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia suaviza o On-Balance Volume (OBV) com uma média móvel selecionável e gera uma linha de sinal. As operações ocorrem quando o OBV suavizado cruza a linha de sinal. Além disso, a estratégia registra divergências regulares e ocultas entre o preço e o OBV usando detecção de fractais.

## Detalhes

- **Critérios de entrada**: OBV-M cruza acima/abaixo da linha de sinal.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: OBV, MA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
