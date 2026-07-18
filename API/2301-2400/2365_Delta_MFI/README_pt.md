# Estratégia Delta MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na comparação dos valores rápido e lento do Índice de Fluxo de Dinheiro (MFI). Vai comprado quando o MFI rápido sobe acima do MFI lento enquanto o MFI lento está acima do nível de sinal. Vai vendido quando o MFI rápido cai abaixo do MFI lento enquanto o MFI lento está abaixo de 100 menos o nível de sinal.

## Detalhes

- **Critérios de entrada**: 
  - Comprar quando `slow MFI > Level` e `fast MFI > slow MFI`
  - Vender quando `slow MFI < 100 - Level` e `fast MFI < slow MFI`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = velas de 4 horas
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: Money Flow Index
  - Stops: Não
  - Complexidade: Básico
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
