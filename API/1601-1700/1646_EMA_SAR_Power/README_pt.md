# Estratégia EMA SAR Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia intradiária combina médias móveis exponenciais rápidas e lentas com Parabolic SAR e indicadores Bulls/Bears Power. Opera apenas durante as horas ativas do mercado e requer margem livre suficiente antes de entrar em qualquer posição.

O sistema vai vendido quando a EMA rápida está abaixo da EMA lenta, o Parabolic SAR fica acima da máxima da vela e o Bears Power está subindo enquanto permanece negativo. Vai comprado quando a EMA rápida está acima da EMA lenta, o Parabolic SAR está abaixo da mínima da vela e o Bulls Power está caindo mas ainda positivo. Cada operação coloca um stop-loss amplo e um take-profit mais próximo.

**Filtro Dinâmico de Margem**

Antes de operar, a estratégia verifica a margem livre da carteira. Dependendo do seu valor, a margem mínima exigida aumenta gradualmente: 600 → 1000 → 1300 → 1500 → 1800 → 2000 → 2500. A operação é ignorada quando a margem livre cai abaixo do limite atual.

## Detalhes

- **Critérios de entrada**:
  - **Vendido**: `EMA3 < EMA34` && `SAR > High` && `BearsPower < 0` && `BearsPower > BearsPower[1]`.
  - **Comprado**: `EMA3 > EMA34` && `SAR < Low` && `BullsPower > 0` && `BullsPower < BullsPower[1]`.
- **Comprado/Vendido**: Ambos os lados.
- **Stop/Alvo**: Stop-loss em 2000 pontos, take-profit em 400 pontos.
- **Filtro de tempo**: Opera apenas entre 09:00 e 16:59 horário do corretor.
- **Indicadores**:
  - Médias Móveis Exponenciais (3, 34) sobre preço mediano.
  - Parabolic SAR (passo 0.02, máximo 0.2).
  - Bulls Power (13) e Bears Power (13).
- **Volume padrão**: 30 contratos.
- **Período**: Velas de 15 minutos.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
