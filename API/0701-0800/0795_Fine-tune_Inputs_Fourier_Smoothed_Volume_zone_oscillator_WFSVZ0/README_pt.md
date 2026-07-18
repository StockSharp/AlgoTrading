# Estratégia de Oscilador de Zona de Volume Suavizado por Fourier WFSVZ0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza um Oscilador de Zona de Volume suavizado por Fourier. Abre comprado quando o oscilador sobe acima do limiar e vendido quando cai abaixo do limiar negativo. Opcionalmente fecha posições abertas quando não há sinal.

## Detalhes

- **Critérios de entrada**: Oscilador sobe acima do limiar / cai abaixo do limiar negativo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou fechamento opcional de todas as posições.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: Volume Zone Oscillator
  - Stops: Nenhum
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
