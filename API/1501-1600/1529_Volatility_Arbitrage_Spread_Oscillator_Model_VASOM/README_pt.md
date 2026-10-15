# Modelo de Oscilador de Spread de Arbitragem de Volatilidade (VASOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Fica comprado no futuro VIX do mês frontal quando o RSI do spread entre os contratos do primeiro e segundo mês cai abaixo de um limiar. A posição é fechada quando o RSI sobe acima de um nível de saída.

## Detalhes
- **Critérios de entrada**: RSI do spread < `LongThreshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: RSI do spread > `ExitThreshold`.
- **Stops**: Não.
- **Valores padrão**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Somente comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
