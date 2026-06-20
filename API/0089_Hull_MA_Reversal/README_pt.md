# Estratégia de Reversão Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Hull Moving Average responde rapidamente às mudanças de preço enquanto permanece suave. Uma mudança em sua direção pode antecipar uma reversão de curto prazo. Esta estratégia monitora valores consecutivos da Hull MA e opera quando a inclinação muda.

Os testes indicam um retorno anual médio de aproximadamente 154%. Funciona melhor no mercado de ações.

Quando a média móvel passa de caindo para subindo, uma posição comprada é aberta. Uma mudança de subindo para caindo inicia uma posição vendida. O risco é controlado usando um stop baseado em ATR colocado além do candle recente.

As saídas dependem desse stop de proteção, capturando uma porção do movimento que segue a mudança de momentum destacada pela Hull MA.

## Detalhes

- **Critérios de entrada**: A inclinação da Hull MA muda de direção.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `HmaPeriod` = 9
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Hull MA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

