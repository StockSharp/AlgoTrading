# Estratégia de Modelo de Banda de Volatilidade Dinâmica Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa bandas de volatilidade estilo Bollinger para operar cruzamentos e sair após um número fixo de velas.

## Detalhes

- **Critérios de entrada**: comprado quando o preço cruza acima da banda inferior; vendido quando o preço cruza abaixo da banda superior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: posição fechada após `ExitBars` velas
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 5
  - `Multiplier` = 1.67
  - `ExitBars` = 7
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: BollingerBands
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
